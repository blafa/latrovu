namespace Latrovu.Intake;

public static class AutomaticMail
{
    private static readonly string[] AutomaticSenders = ["mailer-daemon", "postmaster", "noreply", "no-reply", "donotreply", "do-not-reply"];
    private static readonly string[] BulkPrecedences = ["bulk", "junk", "list", "auto_reply"];

    public static bool IsAutomatic(string? autoSubmitted, string? precedence, bool hasAutoReplyHeader, string sender)
    {
        if (!string.IsNullOrWhiteSpace(autoSubmitted) && !autoSubmitted.Trim().Equals("no", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (hasAutoReplyHeader)
        {
            return true;
        }

        if (precedence is not null && BulkPrecedences.Contains(precedence.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        string localPart = (sender ?? string.Empty).Split('@')[0];
        return AutomaticSenders.Contains(localPart, StringComparer.OrdinalIgnoreCase);
    }
}
