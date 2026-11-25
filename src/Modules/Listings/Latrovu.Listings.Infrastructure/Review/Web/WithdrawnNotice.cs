namespace Latrovu.Listings.Infrastructure.Review.Web;

internal static class WithdrawnNotice
{
    public static string Render() =>
        "<main style=\"grid-column:1 / -1;justify-self:center;align-self:start;width:100%;max-width:560px;box-sizing:border-box;margin:72px auto;padding:0 24px;"
        + "display:grid;gap:14px;font-family:Outfit,system-ui,sans-serif;color:#111418;\">"
        + SiteAssets.LogoImage(36)
        + "<h1 style=\"margin:0;font-size:24px;\">Inserat zurückgezogen und gelöscht</h1>"
        + "<p style=\"margin:0;color:#5E6660;line-height:1.55;\">Das Inserat, seine Bilder, Fragen, Besichtigungstermine und alle Links für Interessierte sind gelöscht. "
        + "Ihre E-Mails dazu entfernen wir ebenfalls aus unserem Postfach.</p>"
        + "<p style=\"margin:0;color:#5E6660;line-height:1.55;\">Möchten Sie die Wohnung wieder anbieten, senden Sie das Exposé einfach erneut an inserat@latrovu.com.</p>"
        + "</main>";
}
