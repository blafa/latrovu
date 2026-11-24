using System.Globalization;
using System.Text;
using Latrovu.BuildingBlocks.Email;
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

    public async Task SendAlertAsync(SeekerAlert alert, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(alert);

        if (alert.Channel != EmailChannel)
        {
            logger.LogWarning("Channel {Channel} is not supported yet, alert for listing {ListingId} not sent", alert.Channel, alert.Listing.ListingId);
            return;
        }

        SeekerTexts texts = SeekerTextCatalog.For(alert.Language);
        string url = $"{alertOptions.Value.ViewBaseUrl.TrimEnd('/')}{ViewPath}{alert.Token}";
        string subject = string.Format(CultureInfo.InvariantCulture, texts.AlertSubject, alert.SearchName);
        await emailSender.SendAsync(
            new EmailMessage(
                alert.Address,
                subject,
                SeekerAlertMail.Text(alert, texts, url),
                SeekerAlertMail.Html(alert, texts, url),
                alertOptions.Value.ReplyToAddress),
            ct);
        logger.LogInformation("Alert for listing {ListingId} sent in {Language}", alert.Listing.ListingId, alert.Language);
    }

    public async Task SendReplyAsync(SeekerReply reply, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(reply);

        if (reply.Channel != EmailChannel)
        {
            logger.LogWarning("Channel {Channel} is not supported yet, reply to {SearchCount} searches not sent", reply.Channel, reply.OpenSearches.Count);
            return;
        }

        SeekerTexts texts = SeekerTextCatalog.For(reply.Language);
        SeekerReply withPlaceQuestions = reply with
        {
            Refinements = [.. PlaceQuestions(reply.Places, texts), .. reply.Refinements],
            Summary = StatusSummary(reply.StatusChanges, texts) ?? reply.Summary
        };
        await emailSender.SendAsync(
            new EmailMessage(
                reply.Address,
                texts.Subject,
                ComposeBody(withPlaceQuestions, texts),
                SeekerReplyHtml.Render(withPlaceQuestions, texts),
                alertOptions.Value.ReplyToAddress),
            ct);
        logger.LogInformation("Search reply sent in {Language} with {SearchCount} open searches", reply.Language, reply.OpenSearches.Count);
    }

    private static string? StatusSummary(IReadOnlyList<Search>? changes, SeekerTexts texts)
    {
        if (changes is not { Count: > 0 })
        {
            return null;
        }

        IEnumerable<string> sentences = changes
            .GroupBy(search => search.Status)
            .Select(group => string.Format(
                CultureInfo.InvariantCulture,
                group.Key switch
                {
                    Search.StoppedStatus => texts.StoppedSummary,
                    Search.PausedStatus => texts.PausedSummary,
                    _ => texts.ResumedSummary
                },
                string.Join(", ", group.Select(search => search.Name))));
        return string.Join(' ', sentences);
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
