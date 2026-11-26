using System.Net;
using Latrovu.BuildingBlocks.Email;
using Latrovu.BuildingBlocks.Infrastructure.Email;
using Latrovu.Listings.Viewing.Viewings;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Latrovu.Listings.Infrastructure.Viewing.Viewings;

public sealed class EmailViewingNotifier(
    NpgsqlDataSource dataSource,
    IEmailSender emailSender,
    IListingAccessTokenStore accessTokens,
    IOptions<PublicLinkOptions> links,
    ILogger<EmailViewingNotifier> logger) : IViewingNotifier
{
    private const string FontStack = "Outfit,Helvetica,Arial,sans-serif";
    private const string NoAddress = "ohne E-Mail-Adresse";
    private const string BookedHighlight = "#EEF8D2";
    private const string CancelledHighlight = "#FCF1DE";

    private const string LookupSql = """
        SELECT l.source_email, coalesce(l.data->>'title', l.file_name, 'Ihr Inserat'),
               (SELECT v.seeker_address FROM listing_view_links v WHERE v.token_hash = $2)
        FROM listings l WHERE l.id = $1
        """;

    public Task NotifyAgentOfBookingAsync(Guid listingId, ViewingSlot slot, string tokenHash, CancellationToken ct) =>
        NotifyAsync(listingId, slot, tokenHash, new AgentViewingNotice("Neue Anmeldung zur Besichtigung", "hat sich jemand zur Besichtigung angemeldet", BookedHighlight), ct);

    public Task NotifyAgentOfRescheduleAsync(Guid listingId, ViewingSlot slot, ViewingSlot previous, string tokenHash, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(previous);

        return NotifyAsync(
            listingId,
            slot,
            tokenHash,
            new AgentViewingNotice("Besichtigung verschoben", "hat jemand die Besichtigung verschoben", BookedHighlight, SwissTime.Describe(previous)),
            ct);
    }

    public Task NotifyAgentOfCancellationAsync(Guid listingId, ViewingSlot slot, string tokenHash, CancellationToken ct) =>
        NotifyAsync(listingId, slot, tokenHash, new AgentViewingNotice("Besichtigung abgesagt", "hat jemand die Besichtigung abgesagt", CancelledHighlight), ct);

    private async Task NotifyAsync(Guid listingId, ViewingSlot slot, string tokenHash, AgentViewingNotice notice, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(slot);

        (string Agent, string Title, string Seeker)? details = await LookupAsync(listingId, tokenHash, ct);
        if (details is not { } found)
        {
            logger.LogWarning("Listing {ListingId} not found, viewing notice \"{Notice}\" not sent", listingId, notice.Subject);
            return;
        }

        string when = SwissTime.Describe(slot);
        string reviewUrl = links.Value.ReviewUrl(await accessTokens.CreateAsync(listingId, ct));
        string text = $"Guten Tag\n\nFür Ihr Inserat «{found.Title}» {notice.Sentence}:\n\n{when}\n{PreviousLine(notice)}{found.Seeker}\n\n"
            + $"Angemeldet: {slot.Booked} von {slot.Capacity} Plätzen.\nAlle Anmeldungen: {reviewUrl}\n\nFreundliche Grüsse\nIhr latrovu-Team";
        await emailSender.SendAsync(
            new EmailMessage(found.Agent, $"{notice.Subject}: {when}", text, Html(found, when, slot, notice, reviewUrl), null, EmailBranding.InlineImages),
            ct);
        logger.LogInformation("Viewing notice \"{Notice}\" for listing {ListingId} sent", notice.Subject, listingId);
    }

    private async Task<(string Agent, string Title, string Seeker)?> LookupAsync(Guid listingId, string tokenHash, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(LookupSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = tokenHash });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return (reader.GetString(0), reader.GetString(1), await reader.IsDBNullAsync(2, ct) ? NoAddress : reader.GetString(2));
    }

    private static string Html((string Agent, string Title, string Seeker) found, string when, ViewingSlot slot, AgentViewingNotice notice, string reviewUrl) =>
        "<!doctype html><html lang=\"de-CH\"><body style=\"margin:0;padding:0;background:#F3F4F1;\">"
        + "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#F3F4F1;padding:32px 16px;\"><tr><td align=\"center\">"
        + $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:520px;background:#FFFFFF;border-radius:6px;font-family:{FontStack};color:#111418;font-size:15px;line-height:1.55;\">"
        + EmailBranding.HeaderRow
        + Row($"<p style=\"margin:0;\">Guten Tag</p><p style=\"margin:12px 0 0;\">Für Ihr Inserat «{Encode(found.Title)}» {Encode(notice.Sentence)}:</p>")
        + Row($"<div style=\"background:{notice.Highlight};border-radius:4px;padding:14px 16px;\"><p style=\"margin:0;font-weight:600;\">{Encode(when)}</p>"
            + (notice.PreviousWhen is null ? string.Empty : $"<p style=\"margin:4px 0 0;color:#5E6660;\">{Encode(PreviousLine(notice).Trim())}</p>")
            + $"<p style=\"margin:4px 0 0;\">{Encode(found.Seeker)}</p></div>")
        + Row($"<p style=\"margin:0;color:#5E6660;font-size:13px;\">Angemeldet: {slot.Booked} von {slot.Capacity} Plätzen.</p>")
        + Row($"<a href=\"{Encode(reviewUrl)}\" style=\"display:inline-block;background:#B8E62E;color:#111418;font-weight:600;text-decoration:none;padding:12px 20px;border-radius:6px;\">Alle Anmeldungen ansehen</a>")
        + Row("<p style=\"margin:0 0 8px;\">Freundliche Grüsse<br>Ihr latrovu-Team</p>")
        + "</table></td></tr></table></body></html>";

    private static string PreviousLine(AgentViewingNotice notice) =>
        notice.PreviousWhen is null ? string.Empty : $"Bisher: {notice.PreviousWhen}\n";

    private static string Row(string content) => $"<tr><td style=\"padding:16px 32px 0;\">{content}</td></tr>";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
