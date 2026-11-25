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
        int end = Enumerable.Range(0, lines.Length).FirstOrDefault(index => IsQuoteStart(lines, index), -1);
        IEnumerable<string> ownLines = end < 0 ? lines : lines.Take(end);
        return string.Join('\n', ownLines).Trim();
    }

    private static bool IsQuoteStart(string[] lines, int index)
    {
        string line = lines[index].Trim();
        if (line.StartsWith('>') || QuoteHeader().IsMatch(line))
        {
            return true;
        }

        string? next = index + 1 < lines.Length ? lines[index + 1].Trim() : null;
        return WrappedHeaderStart().IsMatch(line) && next is not null && QuoteHeader().IsMatch($"{line} {next}");
    }

    [GeneratedRegex(
        @"^(-{2,}\s*(Original Message|Urspr\u00fcngliche Nachricht|Message d'origine|Messaggio originale)\s*-{2,}"
        + @"|(Am|On|Le|Il)\s.+(schrieb|geschrieben|wrote|a \u00e9crit|ha scritto)\b.*"
        + @"|(Von|From|De|Da)\s*:\s*.+)$",
        RegexOptions.IgnoreCase)]
    private static partial Regex QuoteHeader();

    [GeneratedRegex(@"^(Am|On|Le|Il)\s.*\d", RegexOptions.IgnoreCase)]
    private static partial Regex WrappedHeaderStart();
}
