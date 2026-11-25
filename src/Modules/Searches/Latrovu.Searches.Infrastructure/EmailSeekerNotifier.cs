using System.Globalization;
using System.Text;
using Latrovu.BuildingBlocks.Email;
using Latrovu.BuildingBlocks.Infrastructure.Email;
using Microsoft.Extensions.Options;

namespace Latrovu.Searches.Infrastructure;

public sealed class EmailSeekerNotifier(
    IEmailSender emailSender,
    IOptions<AlertOptions> alertOptions,
    ILogger<EmailSeekerNotifier> logger) : ISeekerNotifier
{
    private const string EmailChannel = "email";
    private const string ListPrefix = "– ";
    private const string ViewPath = "/i/";

    public async Task<string?> SendAlertAsync(SeekerAlert alert, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(alert);

        if (alert.Channel != EmailChannel)
        {
            logger.LogWarning("Channel {Channel} is not supported yet, alert for listing {ListingId} not sent", alert.Channel, alert.Listing.ListingId);
            return null;
        }

        SeekerTexts texts = SeekerTextCatalog.For(alert.Language);
        string url = $"{alertOptions.Value.ViewBaseUrl.TrimEnd('/')}{ViewPath}{alert.Token}";
        string subject = string.Format(CultureInfo.InvariantCulture, texts.AlertSubject, alert.SearchName);
        string messageId = await SendAsync(alert.Address, subject, SeekerAlertMail.Text(alert, texts, url), SeekerAlertMail.Html(alert, texts, url), ct);
        logger.LogInformation("Alert for listing {ListingId} sent in {Language}", alert.Listing.ListingId, alert.Language);
        return messageId;
    }

    public async Task SendAgentAnswerAsync(SeekerAgentAnswer answer, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(answer);

        if (answer.Channel != EmailChannel)
        {
            logger.LogWarning("Channel {Channel} is not supported yet, agent answer not sent", answer.Channel);
            return;
        }

        SeekerTexts texts = SeekerTextCatalog.For(answer.Language);
        await SendAsync(
            answer.Address,
            SeekerAgentAnswerMail.Subject(answer, texts),
            SeekerAgentAnswerMail.Text(answer, texts),
            SeekerAgentAnswerMail.Html(answer, texts),
            ct);
        logger.LogInformation("Agent answer sent in {Language}", answer.Language);
    }

    public async Task<string?> SendViewingConfirmationAsync(SeekerViewingConfirmation confirmation, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(confirmation);

        if (confirmation.Channel != EmailChannel)
        {
            logger.LogWarning("Channel {Channel} is not supported yet, viewing confirmation not sent", confirmation.Channel);
            return null;
        }

        SeekerTexts texts = SeekerTextCatalog.For(confirmation.Language);
        string messageId = await SendAsync(
            confirmation.Address,
            SeekerViewingConfirmationMail.Subject(confirmation, texts),
            SeekerViewingConfirmationMail.Text(confirmation, texts),
            SeekerViewingConfirmationMail.Html(confirmation, texts),
            ct);
        logger.LogInformation("Viewing confirmation sent in {Language}", confirmation.Language);
        return messageId;
    }

    public async Task<string?> SendReplyAsync(SeekerReply reply, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(reply);

        if (reply.Channel != EmailChannel)
        {
            logger.LogWarning("Channel {Channel} is not supported yet, reply to {SearchCount} searches not sent", reply.Channel, reply.OpenSearches.Count);
            return null;
        }

        SeekerTexts texts = SeekerTextCatalog.For(reply.Language);
        SeekerReply composed = reply with
        {
            Refinements = [.. PlaceQuestions(reply.Places, texts), .. reply.Refinements],
            Summary = SeekerReplySummary.Compose(reply, texts)
        };
        string messageId = await SendAsync(reply.Address, texts.Subject, ComposeBody(composed, texts), SeekerReplyHtml.Render(composed, texts), ct);
        logger.LogInformation("Search reply sent in {Language} with {SearchCount} open searches", reply.Language, reply.OpenSearches.Count);
        return messageId;
    }

    private async Task<string> SendAsync(string address, string subject, string text, string html, CancellationToken ct)
    {
        string messageId = NewMessageId();
        await emailSender.SendAsync(
            new EmailMessage(address, subject, text, html, alertOptions.Value.ReplyToAddress, EmailBranding.InlineImages, messageId),
            ct);
        return messageId;
    }

    private string NewMessageId()
    {
        string replyTo = alertOptions.Value.ReplyToAddress;
        return $"{Guid.NewGuid():N}@{replyTo[(replyTo.IndexOf('@', StringComparison.Ordinal) + 1)..]}";
    }

    private static IEnumerable<string> PlaceQuestions(PlaceResolution places, SeekerTexts texts) =>
        places.Ambiguous
            .Select(choice => string.Format(CultureInfo.InvariantCulture, texts.AmbiguousPlace, choice.Name, string.Join(" / ", choice.Options)))
            .Concat(places.Unknown.Select(name => string.Format(CultureInfo.InvariantCulture, texts.UnknownPlace, name)));

    private static string ComposeBody(SeekerReply reply, SeekerTexts texts)
    {
        StringBuilder body = new($"{texts.Greeting}\n\n");
        body.Append(reply.Understood ? $"{texts.Understood}\n{reply.Summary}\n\n" : $"{texts.NotUnderstood}\n\n");
        AppendList(body, texts.Refinements, reply.Refinements);
        AppendList(body, texts.OpenSearches, reply.OpenSearches.Select(search => SearchLine(search, texts)).ToList());
        return body.Append($"{texts.HowToChange}\n\n{texts.Closing}\n").ToString();
    }

    private static void AppendList(StringBuilder body, string title, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        body.Append($"{title}\n");
        foreach (string item in items)
        {
            body.Append($"{ListPrefix}{item}\n");
        }

        body.Append('\n');
    }

    private static string SearchLine(Search search, SeekerTexts texts) =>
        search.Status == Search.PausedStatus ? $"{search.Name} ({texts.Paused})" : search.Name;
}
