namespace Latrovu.Listings.Infrastructure.Review.Web;

internal static class SiteAssets
{
    public const string HtmxScript = "/assets/htmx.min.js";
    public const string Logo = "/assets/latrovu-logo.svg";

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

    public static string LogoImage(int height) =>
        $"<img class=\"brand\" src=\"{Logo}\" alt=\"latrovu\" height=\"{height}\" style=\"display:block;width:auto\">";
}
