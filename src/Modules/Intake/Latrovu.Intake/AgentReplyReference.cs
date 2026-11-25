using System.Text.RegularExpressions;

namespace Latrovu.Intake;

public static partial class AgentReplyReference
{
    private const string Prefix = "Q-";

    public static string? Find(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        Match match = ReferencePattern().Match(subject);
        return match.Success ? Prefix + match.Groups[1].Value.ToLowerInvariant() : null;
    }

    [GeneratedRegex(@"\[Q-([0-9a-f]{10})\]", RegexOptions.IgnoreCase)]
    private static partial Regex ReferencePattern();
}
