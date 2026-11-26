using System.Globalization;
using Latrovu.BuildingBlocks.Email;
using Latrovu.BuildingBlocks.Infrastructure.Email;

namespace Latrovu.Listings.Infrastructure.Access;

public sealed class EmailVerificationCodeSender(IEmailSender emailSender) : IVerificationCodeSender
{
    private const string SubjectTemplate = "{0} ist Ihr Bestätigungscode für latrovu";

    private const string TextTemplate = """
        Guten Tag

        Ihr Code zum Öffnen Ihres Inserats bei latrovu lautet:

            {0}

        Der Code ist {1} Minuten gültig. Falls Sie ihn nicht angefordert haben, können Sie diese E-Mail ignorieren.

        Freundliche Grüsse
        latrovu
        """;

    private const string HtmlTemplate = """
        <!doctype html>
        <html lang="de-CH">
        <body style="margin:0;padding:0;background:#F3F4F1;">
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#F3F4F1;padding:32px 16px;">
        <tr><td align="center">
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:440px;background:#FFFFFF;border-radius:6px;font-family:Outfit,Helvetica,Arial,sans-serif;color:#111418;">
        {2}
        <tr><td style="padding:24px 32px 0;font-size:15px;line-height:1.5;">Ihr Code zum Öffnen Ihres Inserats:</td></tr>
        <tr><td style="padding:16px 32px;">
        <div style="background:#F6F7F4;border-radius:4px;padding:18px 0;text-align:center;font-size:32px;font-weight:700;letter-spacing:10px;font-family:Menlo,Consolas,monospace;">{0}</div>
        </td></tr>
        <tr><td style="padding:0 32px 28px;font-size:13px;line-height:1.5;color:#5E6660;">Der Code ist {1} Minuten gültig. Falls Sie ihn nicht angefordert haben, können Sie diese E-Mail ignorieren.</td></tr>
        </table>
        <p style="margin:16px 0 0;font-family:Helvetica,Arial,sans-serif;font-size:12px;color:#5E6660;">latrovu – Te la trovo.</p>
        </td></tr>
        </table>
        </body>
        </html>
        """;

    public Task SendAsync(string recipient, string code, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        double minutes = ReviewAccessService.CodeValidity.TotalMinutes;
        EmailMessage message = new(
            recipient,
            string.Format(CultureInfo.InvariantCulture, SubjectTemplate, code),
            string.Format(CultureInfo.InvariantCulture, TextTemplate, code, minutes),
            string.Format(CultureInfo.InvariantCulture, HtmlTemplate, code, minutes, EmailBranding.HeaderRow),
            InlineImages: EmailBranding.InlineImages);
        return emailSender.SendAsync(message, ct);
    }
}
