using System.Net;
using System.Text;

namespace Latrovu.Listings.Infrastructure.Review.Web;

public static class ReviewGatePage
{
    private const string PageTitle = "Inserat öffnen";
    private const int CodeLength = 6;

    private const string CodeScript = """
        <script>
        function digitsOf(container) { return [...container.querySelectorAll('[data-digit]')]; }
        function syncCode(container) {
            const form = container.closest('form');
            const code = digitsOf(container).map(input => input.value).join('');
            form.querySelector('input[name=code]').value = code;
            if (code.length === 6) { form.requestSubmit(); }
        }
        function fillFrom(input, text) {
            const container = input.closest('[data-code]');
            const digits = digitsOf(container);
            const start = digits.indexOf(input);
            text.replace(/\D/g, '').split('').forEach((digit, offset) => {
                if (digits[start + offset]) { digits[start + offset].value = digit; }
            });
            (digits.find(candidate => !candidate.value) ?? digits[digits.length - 1]).focus();
            syncCode(container);
        }
        document.addEventListener('input', event => {
            const input = event.target.closest('[data-digit]');
            if (!input) { return; }
            const typed = input.value;
            input.value = '';
            fillFrom(input, typed);
        });
        document.addEventListener('paste', event => {
            const input = event.target.closest('[data-digit]');
            if (!input) { return; }
            event.preventDefault();
            fillFrom(input, event.clipboardData.getData('text'));
        });
        document.addEventListener('keydown', event => {
            const input = event.target.closest('[data-digit]');
            if (!input) { return; }
            const digits = digitsOf(input.closest('[data-code]'));
            const index = digits.indexOf(input);
            if (event.key === 'Backspace' && !input.value && index > 0) { digits[index - 1].value = ''; digits[index - 1].focus(); }
            if (event.key === 'ArrowLeft' && index > 0) { digits[index - 1].focus(); }
            if (event.key === 'ArrowRight' && index < digits.length - 1) { digits[index + 1].focus(); }
        });
        document.body.addEventListener('htmx:afterSwap', () => document.querySelector('[data-digit]')?.focus());
        </script>
        """;

    public static string RenderGate(string token)
    {
        string encodedToken = WebUtility.HtmlEncode(token);
        StringBuilder html = new();
        ReviewLayout.AppendHead(html, PageTitle);
        html.Append("<main><div class=\"gate\"><h1>Inserat öffnen</h1>")
            .Append("<p>Zum Schutz Ihrer Daten senden wir Ihnen einen Bestätigungscode ")
            .Append("an die E-Mail-Adresse, von der das Inserat eingereicht wurde.</p>")
            .Append($"<div id=\"gate\"><button class=\"primary\" hx-post=\"/review/{encodedToken}/code\" hx-target=\"#gate\">Code senden</button></div>")
            .Append("</div></main>")
            .Append(CodeScript);
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
            .Append($"<form class=\"code-form\" hx-post=\"/review/{encodedToken}/verify\" hx-target=\"#gate-message\">")
            .Append(DigitInputs())
            .Append("<input type=\"hidden\" name=\"code\">")
            .Append("<button type=\"submit\" class=\"primary\">Bestätigen</button></form>")
            .Append("<div id=\"gate-message\" class=\"status\" aria-live=\"polite\"></div>")
            .Append($"<button class=\"secondary\" hx-post=\"/review/{encodedToken}/code\" hx-target=\"#gate\">Neuen Code senden</button>")
            .ToString();
    }

    private static string DigitInputs()
    {
        StringBuilder inputs = new("<div class=\"code-inputs\" data-code role=\"group\" aria-label=\"Bestätigungscode\">");
        for (int position = 1; position <= CodeLength; position++)
        {
            string autocomplete = position == 1 ? " autocomplete=\"one-time-code\"" : " autocomplete=\"off\"";
            inputs.Append($"<input type=\"text\" inputmode=\"numeric\" data-digit{autocomplete} aria-label=\"Ziffer {position} von {CodeLength}\">");
        }

        return inputs.Append("</div>").ToString();
    }

    private static string Paragraph(string text) => $"<p>{WebUtility.HtmlEncode(text)}</p>";
}
