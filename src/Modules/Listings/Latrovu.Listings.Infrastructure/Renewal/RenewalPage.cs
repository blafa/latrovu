using System.Text;
using Latrovu.Listings.Infrastructure.Review.Web;

namespace Latrovu.Listings.Infrastructure.Renewal;

internal static class RenewalPage
{
    private const string Styles = """
        body { margin:0; background:#F3F4F1; color:#111418; font:16px/1.55 Outfit, system-ui, sans-serif; }
        main { max-width:520px; margin:72px auto; padding:0 24px; display:grid; gap:14px; }
        h1 { margin:0; font-size:24px; line-height:1.2; }
        p { margin:0; color:#5E6660; }
        button { justify-self:start; font:inherit; font-weight:600; padding:12px 20px; border:none; border-radius:8px; background:#B8E62E; color:#111418; cursor:pointer; }
        """;

    public static string Ask() =>
        Page(
            "Ist Ihr Inserat noch aktuell?",
            "<p>Bestätigen Sie, dass Ihr Angebot weiterhin aktuell ist. Wir fragen in einem Monat wieder nach.</p>"
            + "<form method=\"post\"><button type=\"submit\">Ja, weiterhin aktiv</button></form>");

    public static string Done() =>
        Page("Danke, Ihr Inserat bleibt aktiv", "<p>Wir erinnern Sie in einem Monat wieder. Ist das Angebot vorher vergeben, ziehen Sie das Inserat einfach zurück.</p>");

    public static string Invalid() =>
        Page("Dieser Link ist nicht mehr gültig", "<p>Das Inserat wurde bereits gelöscht oder der Link ist abgelaufen.</p>");

    private static string Page(string title, string body) =>
        new StringBuilder("<!doctype html><html lang=\"de-CH\"><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><meta name=\"robots\" content=\"noindex, nofollow\">")
            .Append($"<title>{title} – latrovu</title>{SiteAssets.HeadLinks}<style>{SiteAssets.FontFaces}{Styles}</style></head><body><main>")
            .Append(SiteAssets.LogoImage(SiteAssets.HeaderLogoHeight))
            .Append($"<h1>{title}</h1>{body}</main></body></html>")
            .ToString();
}
