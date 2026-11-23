using System.Net;
using System.Text;

namespace Latrovu.Listings.Infrastructure.Review.Web;

internal static class ReviewGallery
{
    public static void AppendViewer(StringBuilder html, string token, IReadOnlyList<ListingImageRef> images)
    {
        html.Append($"<div class=\"viewer\"><img id=\"hero-photo\" alt=\"Bild 1 von {images.Count}\" loading=\"lazy\" src=\"{Url(token, images[0])}\">")
            .Append("<button type=\"button\" class=\"nav prev\" data-step=\"-1\" aria-label=\"Vorheriges Bild\">‹</button>")
            .Append("<button type=\"button\" class=\"nav next\" data-step=\"1\" aria-label=\"Nächstes Bild\">›</button>")
            .Append($"<span id=\"photo-counter\" class=\"counter\">1 / {images.Count}</span></div><div class=\"thumbs\">");
        for (int index = 0; index < images.Count; index++)
        {
            string active = index == 0 ? " active" : string.Empty;
            string url = Url(token, images[index]);
            html.Append($"<button type=\"button\" class=\"thumb{active}\" data-full=\"{url}\" aria-label=\"Bild {index + 1} anzeigen\">")
                .Append($"<img loading=\"lazy\" alt=\"\" src=\"{url}\"></button>");
        }

        html.Append("</div>");
    }

    private static string Url(string token, ListingImageRef image) => $"/review/{WebUtility.HtmlEncode(token)}/images/{image.Position}";
}
