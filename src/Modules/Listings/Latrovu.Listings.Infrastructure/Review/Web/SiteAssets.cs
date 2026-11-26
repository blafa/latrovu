using System.Globalization;

namespace Latrovu.Listings.Infrastructure.Review.Web;

internal static class SiteAssets
{
    public const string HtmxScript = "/assets/htmx.min.js";
    public const int HeaderLogoHeight = 44;
    public const int CompactLogoHeight = 38;

    private const double SymbolAspect = 66.0 / 74.0;
    private const double WordmarkScale = 0.74;
    private const double GapScale = 0.26;

    private const string Symbol =
        "<path d=\"M10 18 H32 A10 10 0 0 1 42 28 V50 A10 10 0 0 1 32 60 H18 L6 68 L10 60 A10 10 0 0 1 0 50 V28 A10 10 0 0 1 10 18 Z\" fill=\"#B8E62E\"/>"
        + "<path d=\"M50 0 H28 A10 10 0 0 0 18 10 V32 A10 10 0 0 0 28 42 H42 L54 50 L50 42 A10 10 0 0 0 60 32 V10 A10 10 0 0 0 50 0 Z\" fill=\"none\" stroke=\"#111418\" stroke-width=\"4\" stroke-linejoin=\"round\"/>"
        + "<path d=\"M26.2 30.3 A5 5 0 1 1 33.8 30.3 L34 37.6 H26 Z\" fill=\"#111418\"/>";

    public const string HeadLinks =
        "<link rel=\"icon\" href=\"/assets/favicon.svg\" type=\"image/svg+xml\">"
        + "<link rel=\"icon\" href=\"/assets/favicon-32.png\" sizes=\"32x32\" type=\"image/png\">"
        + "<link rel=\"apple-touch-icon\" href=\"/assets/favicon-180.png\">";

    public const string FontFaces = """
        @font-face { font-family:Outfit; font-style:normal; font-weight:400; font-display:swap; src:url(/assets/fonts/outfit-latin-400-normal.woff2) format("woff2"); }
        @font-face { font-family:Outfit; font-style:normal; font-weight:500; font-display:swap; src:url(/assets/fonts/outfit-latin-500-normal.woff2) format("woff2"); }
        @font-face { font-family:Outfit; font-style:normal; font-weight:600; font-display:swap; src:url(/assets/fonts/outfit-latin-600-normal.woff2) format("woff2"); }
        @font-face { font-family:Outfit; font-style:normal; font-weight:700; font-display:swap; src:url(/assets/fonts/outfit-latin-700-normal.woff2) format("woff2"); }
        """;

    public static string LogoImage(int height, string cssClass = "brand")
    {
        string width = (height * SymbolAspect).ToString("0.#", CultureInfo.InvariantCulture);
        string fontSize = (height * WordmarkScale).ToString("0.#", CultureInfo.InvariantCulture);
        string gap = (height * GapScale).ToString("0.#", CultureInfo.InvariantCulture);
        return $"<span class=\"{cssClass}\" style=\"display:inline-flex;align-items:center;gap:{gap}px;color:#111418;\">"
            + $"<svg viewBox=\"-3 -3 66 74\" width=\"{width}\" height=\"{height}\" aria-hidden=\"true\" focusable=\"false\">{Symbol}</svg>"
            + $"<span style=\"font-family:Outfit,system-ui,sans-serif;font-weight:600;font-size:{fontSize}px;letter-spacing:-0.035em;line-height:1;\">"
            + "latrovu<span style=\"color:#9CC41F;\">.</span></span></span>";
    }
}
