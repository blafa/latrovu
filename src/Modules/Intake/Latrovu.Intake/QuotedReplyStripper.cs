using System.Text.RegularExpressions;

namespace Latrovu.Intake;

public static partial class QuotedReplyStripper
{
    public static string Strip(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        string[] lines = body.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        int end = Array.FindIndex(lines, IsQuoteStart);
        IEnumerable<string> ownLines = end < 0 ? lines : lines.Take(end);
        return string.Join('\n', ownLines).Trim();
    }

    private static bool IsQuoteStart(string line) =>
        line.TrimStart().StartsWith('>') || QuoteHeader().IsMatch(line.Trim());

    [GeneratedRegex(
        @"^(-{2,}\s*(Original Message|Urspr\u00fcngliche Nachricht|Message d'origine|Messaggio originale)\s*-{2,}"
        + @"|(Am|On|Le|Il)\s.+(schrieb|wrote|a \u00e9crit|ha scritto)\b.*"
        + @"|(Von|From|De|Da)\s*:\s*.+)$",
        RegexOptions.IgnoreCase)]
    private static partial Regex QuoteHeader();
}
