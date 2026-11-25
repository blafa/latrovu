using System.Net;
using Latrovu.BuildingBlocks.Email;
using Latrovu.BuildingBlocks.Infrastructure.Email;
using Latrovu.Listings.Viewing.Viewings;
using Npgsql;

namespace Latrovu.Listings.Infrastructure.Viewing.Viewings;

public sealed class EmailViewingNotifier(
    NpgsqlDataSource dataSource,
    IEmailSender emailSender,
    ILogger<EmailViewingNotifier> logger) : IViewingNotifier
{
    private const string FontStack = "Outfit,Helvetica,Arial,sans-serif";
    private const string NoAddress = "ohne E-Mail-Adresse";

    private const string LookupSql = """
        SELECT l.source_email, coalesce(l.data->>'title', l.file_name, 'Ihr Inserat'),
               (SELECT v.seeker_address FROM listing_view_links v WHERE v.token_hash = $2)
        FROM listings l WHERE l.id = $1
        """;

    public async Task NotifyAgentOfBookingAsync(Guid listingId, ViewingSlot slot, string tokenHash, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(slot);

        (string Agent, string Title, string Seeker)? details = await LookupAsync(listingId, tokenHash, ct);
        if (details is not { } found)
        {
            logger.LogWarning("Listing {ListingId} not found, booking notification not sent", listingId);
            return;
        }

        string when = SwissTime.Describe(slot);
        string text = $"Guten Tag\n\nFür Ihr Inserat «{found.Title}» hat sich jemand zur Besichtigung angemeldet:\n\n{when}\n{found.Seeker}\n\n"
            + $"Angemeldet: {slot.Booked} von {slot.Capacity} Plätzen. Alle Anmeldungen sehen Sie auf Ihrer Inseratsseite.\n\nFreundliche Grüsse\nIhr Latrovu-Team";
        await emailSender.SendAsync(
            new EmailMessage(found.Agent, $"Neue Anmeldung zur Besichtigung: {when}", text, Html(found.Title, when, found.Seeker, slot), null, EmailBranding.InlineImages),
            ct);
        logger.LogInformation("Booking notification for listing {ListingId} sent", listingId);
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

    private static string Html(string title, string when, string seeker, ViewingSlot slot) =>
        "<!doctype html><html lang=\"de-CH\"><body style=\"margin:0;padding:0;background:#F3F4F1;\">"
        + "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#F3F4F1;padding:32px 16px;\"><tr><td align=\"center\">"
        + $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:520px;background:#FFFFFF;border-radius:6px;font-family:{FontStack};color:#111418;font-size:15px;line-height:1.55;\">"
        + EmailBranding.HeaderRow
        + Row($"<p style=\"margin:0;\">Guten Tag</p><p style=\"margin:12px 0 0;\">Für Ihr Inserat «{Encode(title)}» hat sich jemand zur Besichtigung angemeldet:</p>")
        + Row($"<div style=\"background:#EEF8D2;border-radius:4px;padding:14px 16px;\"><p style=\"margin:0;font-weight:600;\">{Encode(when)}</p>"
            + $"<p style=\"margin:4px 0 0;\">{Encode(seeker)}</p></div>")
        + Row($"<p style=\"margin:0;color:#5E6660;font-size:13px;\">Angemeldet: {slot.Booked} von {slot.Capacity} Plätzen. Alle Anmeldungen sehen Sie auf Ihrer Inseratsseite.</p>")
        + Row("<p style=\"margin:0 0 8px;\">Freundliche Grüsse<br>Ihr Latrovu-Team</p>")
        + "</table></td></tr></table></body></html>";

    private static string Row(string content) => $"<tr><td style=\"padding:16px 32px 0;\">{content}</td></tr>";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
