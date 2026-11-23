using System.Net;
using System.Text;

namespace Latrovu.Listings.Infrastructure.Review.Web;

public static class ReviewGatePage
{
    private const string PageTitle = "Inserat öffnen";

    public static string RenderGate(string token)
    {
        string encodedToken = WebUtility.HtmlEncode(token);
        StringBuilder html = new();
        ReviewLayout.AppendHead(html, PageTitle);
        html.Append("<div class=\"gate\"><h1>Inserat öffnen</h1>")
            .Append("<p>Zum Schutz Ihrer Daten senden wir Ihnen einen Bestätigungscode ")
            .Append("an die E-Mail-Adresse, von der das Inserat eingereicht wurde.</p>")
            .Append($"<div id=\"gate\"><button hx-post=\"/review/{encodedToken}/code\" hx-target=\"#gate\">Code senden</button></div>")
            .Append("</div>");
        ReviewLayout.AppendEnd(html);
        return html.ToString();
    }

    public static string RenderCodeRequestResult(string token, CodeRequestResult result) => result switch
    {
        CodeRequestResult.Sent => RenderCodeForm(token, "Wir haben Ihnen einen Code gesendet."),
        CodeRequestResult.TooManyRequests => Paragraph(
            $"Zu viele Anfragen. Bitte versuchen Sie es in {ReviewAccessService.CodeRequestWindow.TotalMinutes} Minuten erneut."),
        _ => Paragraph("Dieser Link ist ungültig oder abgelaufen.")
    };

    public static string RenderVerificationFailure(CodeVerificationOutcome outcome) => outcome switch
    {
        CodeVerificationOutcome.Invalid => "Der Code ist nicht korrekt.",
        CodeVerificationOutcome.Expired => "Der Code ist abgelaufen. Bitte fordern Sie einen neuen an.",
        CodeVerificationOutcome.TooManyAttempts => "Zu viele Versuche. Bitte fordern Sie einen neuen Code an.",
        _ => "Dieser Link ist ungültig oder abgelaufen."
    };

    private static string RenderCodeForm(string token, string message)
    {
        string encodedToken = WebUtility.HtmlEncode(token);
        return new StringBuilder()
            .Append(Paragraph(message))
            .Append($"<form hx-post=\"/review/{encodedToken}/verify\" hx-target=\"#gate-message\">")
            .Append("<input name=\"code\" inputmode=\"numeric\" autocomplete=\"one-time-code\" maxlength=\"6\" ")
            .Append("pattern=\"[0-9]{6}\" required aria-label=\"Bestätigungscode\">")
            .Append("<button type=\"submit\">Bestätigen</button></form>")
            .Append("<div id=\"gate-message\" class=\"status\"></div>")
            .Append($"<button class=\"secondary\" hx-post=\"/review/{encodedToken}/code\" hx-target=\"#gate\">Neuen Code senden</button>")
            .ToString();
    }

    private static string Paragraph(string text) => $"<p>{WebUtility.HtmlEncode(text)}</p>";
}
