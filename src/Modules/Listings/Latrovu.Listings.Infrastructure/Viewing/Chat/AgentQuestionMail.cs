using System.Net;
using System.Text;
using Latrovu.BuildingBlocks.Email;
using Latrovu.BuildingBlocks.Infrastructure.Email;

namespace Latrovu.Listings.Infrastructure.Viewing.Chat;

public static class AgentQuestionMail
{
    private const string FontStack = "Outfit,Helvetica,Arial,sans-serif";

    public static EmailMessage Compose(ForwardedQuestionBatch batch, string replyTo)
    {
        ArgumentNullException.ThrowIfNull(batch);

        string subject = $"Anfrage zu Ihrem Inserat «{batch.Title}» [{batch.Reference}]";
        return new EmailMessage(batch.AgentAddress, subject, Text(batch), Html(batch), replyTo, EmailBranding.InlineImages);
    }

    private static string Intro(ForwardedQuestionBatch batch) => batch.Questions.Count == 1
        ? $"Eine interessierte Person hat zu Ihrem Inserat «{batch.Title}» folgendes Anliegen:"
        : $"Eine interessierte Person hat zu Ihrem Inserat «{batch.Title}» folgende Anliegen:";

    private static string Text(ForwardedQuestionBatch batch)
    {
        StringBuilder text = new($"Guten Tag\n\n{Intro(batch)}\n\n");
        foreach (string question in batch.Questions)
        {
            text.Append($"– {question}\n");
        }

        return text.Append("\nAntworten Sie einfach auf diese E-Mail. Wir leiten Ihre Antwort weiter; ")
            .Append("Ihre Kontaktdaten bleiben dabei verborgen.\n\nFreundliche Grüsse\nIhr Latrovu-Team")
            .ToString();
    }

    private static string Html(ForwardedQuestionBatch batch)
    {
        string questions = string.Join(string.Empty, batch.Questions.Select(question =>
            $"<li style=\"margin:0 0 6px;\">{WebUtility.HtmlEncode(question)}</li>"));
        return "<!doctype html><html lang=\"de-CH\"><body style=\"margin:0;padding:0;background:#F3F4F1;\">"
            + "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#F3F4F1;padding:32px 16px;\"><tr><td align=\"center\">"
            + $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:520px;background:#FFFFFF;border-radius:6px;font-family:{FontStack};color:#111418;font-size:15px;line-height:1.55;\">"
            + EmailBranding.HeaderRow
            + Row($"<p style=\"margin:0;\">Guten Tag</p><p style=\"margin:12px 0 0;\">{WebUtility.HtmlEncode(Intro(batch))}</p>")
            + Row($"<ul style=\"margin:0;padding:14px 16px 8px 34px;background:#F6F7F4;border-radius:4px;\">{questions}</ul>")
            + Row("<p style=\"margin:0;\">Antworten Sie einfach auf diese E-Mail. Wir leiten Ihre Antwort weiter; Ihre Kontaktdaten bleiben dabei verborgen.</p>")
            + Row("<p style=\"margin:0 0 8px;\">Freundliche Grüsse<br>Ihr Latrovu-Team</p>")
            + "</table></td></tr></table></body></html>";
    }

    private static string Row(string content) => $"<tr><td style=\"padding:16px 32px 0;\">{content}</td></tr>";
}
