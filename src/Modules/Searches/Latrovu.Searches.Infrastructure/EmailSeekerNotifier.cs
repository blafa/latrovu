using System.Globalization;
using System.Text;
using Latrovu.BuildingBlocks.Email;

namespace Latrovu.Searches.Infrastructure;

public sealed class EmailSeekerNotifier(IEmailSender emailSender, ILogger<EmailSeekerNotifier> logger) : ISeekerNotifier
{
    private const string EmailChannel = "email";
    private const string ListPrefix = "– ";

    public async Task SendReplyAsync(SeekerReply reply, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(reply);

        if (reply.Channel != EmailChannel)
        {
            logger.LogWarning("Channel {Channel} is not supported yet, reply to {SearchCount} searches not sent", reply.Channel, reply.OpenSearches.Count);
            return;
        }

        SeekerTexts texts = SeekerTextCatalog.For(reply.Language);
        SeekerReply withPlaceQuestions = reply with { Refinements = [.. PlaceQuestions(reply.Places, texts), .. reply.Refinements] };
        await emailSender.SendAsync(
            new EmailMessage(
                reply.Address,
                texts.Subject,
                ComposeBody(withPlaceQuestions, texts),
                SeekerReplyHtml.Render(withPlaceQuestions, texts)),
            ct);
        logger.LogInformation("Search reply sent in {Language} with {SearchCount} open searches", reply.Language, reply.OpenSearches.Count);
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
