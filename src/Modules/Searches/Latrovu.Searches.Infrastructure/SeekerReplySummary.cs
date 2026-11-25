using System.Globalization;

namespace Latrovu.Searches.Infrastructure;

public static class SeekerReplySummary
{
    public static string? Compose(SeekerReply reply, SeekerTexts texts)
    {
        ArgumentNullException.ThrowIfNull(reply);
        ArgumentNullException.ThrowIfNull(texts);

        string?[] parts = [StatusSummary(reply.StatusChanges, texts) ?? reply.Summary, MatchSentence(reply.Matches, texts)];
        string joined = string.Join(' ', parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        return joined.Length == 0 ? null : joined;
    }

    private static string? MatchSentence(SearchMatchSummary? matches, SeekerTexts texts)
    {
        if (matches is null)
        {
            return null;
        }

        return matches.News switch
        {
            MatchNews.Sent when matches.Sent == 1 => texts.MatchSentOne,
            MatchNews.Sent => string.Format(CultureInfo.InvariantCulture, texts.MatchesSent, matches.Sent),
            MatchNews.AlreadySent => texts.MatchesAlreadySent,
            _ => texts.NoMatchesYet
        };
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
}
