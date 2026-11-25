using System.Net;
using Latrovu.Listings.Infrastructure.Review.Web;
using Latrovu.Listings.Viewing.Access;

namespace Latrovu.Listings.Infrastructure.Viewing.Web;

internal static class UnlockDialog
{
    public const string Styles = """
        .unlock-open { justify-self:start; font:inherit; font-size:14px; font-weight:600; padding:8px 14px; border:none; border-radius:18px; background:var(--lime); color:var(--ink); cursor:pointer; }
        .unlock-dialog { width:min(440px, calc(100vw - 32px)); padding:24px; border:none; border-radius:16px; background:var(--paper); color:var(--ink); }
        .unlock-dialog::backdrop { background:rgba(17,20,24,0.45); }
        .unlock-dialog h2 { margin:0 32px 6px 0; font-size:19px; }
        .unlock-dialog p { margin:0 0 14px; font-size:14px; color:var(--muted); }
        .unlock-close { position:absolute; top:14px; right:14px; width:34px; height:34px; display:grid; place-items:center; border:none; border-radius:50%; background:var(--well); color:var(--ink); cursor:pointer; }
        .unlock-close .icon { width:18px; height:18px; }
        .unlock-form { display:grid; gap:10px; }
        .unlock-form label { font-size:13px; font-weight:500; }
        .unlock-form input { font:inherit; font-size:16px; padding:10px 12px; border:1px solid var(--line); border-radius:10px; }
        .unlock-form input:focus { outline:none; border-color:var(--ink); }
        .unlock-form button { font:inherit; font-weight:600; padding:11px 16px; border:none; border-radius:10px; background:var(--lime); color:var(--ink); cursor:pointer; }
        .unlock-form button[disabled] { opacity:0.6; cursor:progress; }
        .unlock-error { margin:0; font-size:13px; color:#A32D2D; }
        .unlock-hint { margin:0; font-size:12px; color:var(--muted); }
        .unlock-done { display:grid; gap:8px; }
        .unlock-done strong { font-size:17px; }
        """;

    public const string Script = """
        <script>
        const unlockDialog = document.getElementById('unlock');
        document.body.addEventListener('click', event => {
            if (event.target.closest('[data-unlock]')) { event.preventDefault(); unlockDialog.showModal(); unlockDialog.querySelector('input')?.focus(); return; }
            if (event.target.closest('.unlock-close') || event.target === unlockDialog) { unlockDialog.close(); }
        });
        </script>
        """;

    public static string Render(string token) =>
        "<dialog id=\"unlock\" class=\"unlock-dialog\" aria-labelledby=\"unlock-title\">"
        + $"<button type=\"button\" class=\"unlock-close\" aria-label=\"Schliessen\">{ReviewIcons.Cross}</button>"
        + $"<div id=\"unlock-body\">{Form(token, null)}</div></dialog>";

    public static string Form(string token, string? error) =>
        "<h2 id=\"unlock-title\">Ihr persönlicher Link</h2>"
        + "<p>Damit stellen Sie Fragen zum Angebot und buchen eine Besichtigung.</p>"
        + $"<form class=\"unlock-form\" hx-post=\"/i/{Encode(token)}/access\" hx-target=\"#unlock-body\" hx-swap=\"innerHTML\" hx-disabled-elt=\"find button\">"
        + "<label for=\"unlock-email\">Ihre E-Mail-Adresse</label>"
        + $"<input id=\"unlock-email\" type=\"email\" name=\"email\" required maxlength=\"{ListingAccessService.MaxAddressLength}\" autocomplete=\"email\" inputmode=\"email\">"
        + (error is null ? string.Empty : $"<p class=\"unlock-error\" role=\"alert\">{Encode(error)}</p>")
        + "<button type=\"submit\">Link anfordern</button>"
        + "<p class=\"unlock-hint\">Kein Konto, keine Werbung. Ihre Adresse sieht der Anbieter erst, wenn Sie eine Besichtigung buchen.</p>"
        + "</form>";

    public static string Done() =>
        "<div class=\"unlock-done\" role=\"status\"><strong>Ihr Link ist unterwegs</strong>"
        + "<p>Schauen Sie in Ihr Postfach. Mit dem Link aus der E-Mail fragen und buchen Sie direkt.</p></div>";

    public static string Limited() =>
        "<div class=\"unlock-done\" role=\"status\"><strong>Gerade nicht möglich</strong>"
        + "<p>Für diesen Link wurden schon viele persönliche Links angefordert. Bitte versuchen Sie es später nochmals.</p></div>";

    public static string Result(string token, AccessOutcome outcome) => outcome switch
    {
        AccessOutcome.Sent => Done(),
        AccessOutcome.LimitReached => Limited(),
        _ => Form(token, "Bitte prüfen Sie die E-Mail-Adresse.")
    };

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
