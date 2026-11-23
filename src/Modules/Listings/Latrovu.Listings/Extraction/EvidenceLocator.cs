using System.Text;
using System.Text.RegularExpressions;

namespace Latrovu.Listings.Extraction;

public static partial class EvidenceLocator
{
    public const int ContextCharacters = 150;
    private const string Ellipsis = "…";
    private const string Apostrophes = "['’‘]";
    private const string Dashes = "[-–—]";
    private const string FlexibleWhitespace = @"\s+";
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(200);

    public static EvidenceLocation? Locate(IReadOnlyList<string> pages, string needle)
    {
        ArgumentNullException.ThrowIfNull(pages);

        if (string.IsNullOrWhiteSpace(needle))
        {
            return null;
        }

        Regex pattern = ToPattern(needle.Trim());
        for (int index = 0; index < pages.Count; index++)
        {
            Match match = pattern.Match(pages[index]);
            if (match.Success)
            {
                string page = pages[index];
                return new EvidenceLocation(index + 1, Before(page, match.Index), Tidy(match.Value), After(page, match.Index + match.Length));
            }
        }

        return null;
    }

    private static Regex ToPattern(string needle)
    {
        StringBuilder pattern = new();
        bool previousWasWhitespace = false;
        foreach (char character in needle)
        {
            if (char.IsWhiteSpace(character))
            {
                pattern.Append(previousWasWhitespace ? string.Empty : FlexibleWhitespace);
                previousWasWhitespace = true;
                continue;
            }

            previousWasWhitespace = false;
            pattern.Append(character switch
            {
                '\'' or '’' or '‘' => Apostrophes,
                '-' or '–' or '—' => Dashes,
                _ => Regex.Escape(character.ToString())
            });
        }

        return new Regex(pattern.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, MatchTimeout);
    }

    private static string Before(string page, int matchStart)
    {
        int start = Math.Max(0, matchStart - ContextCharacters);
        string context = page[start..matchStart];
        if (start > 0 && context.IndexOf(' ') is var space and >= 0)
        {
            context = Ellipsis + context[(space + 1)..];
        }

        return Tidy(context, keepEdges: true);
    }

    private static string After(string page, int matchEnd)
    {
        int end = Math.Min(page.Length, matchEnd + ContextCharacters);
        string context = page[matchEnd..end];
        if (end < page.Length && context.LastIndexOf(' ') is var space and > 0)
        {
            context = context[..space] + Ellipsis;
        }

        return Tidy(context, keepEdges: true);
    }

    private static string Tidy(string text, bool keepEdges = false)
    {
        string collapsed = Whitespace().Replace(text, " ");
        return keepEdges ? collapsed : collapsed.Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
