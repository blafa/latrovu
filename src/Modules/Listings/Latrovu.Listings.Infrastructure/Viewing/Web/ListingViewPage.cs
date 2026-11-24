using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Latrovu.Listings.Infrastructure.Review.Web;

namespace Latrovu.Listings.Infrastructure.Viewing.Web;

internal static class ListingViewPage
{
    private const int GalleryPreviewCount = 5;

    private static readonly string[] HeadlinePaths =
        ["size.rooms", "size.livingAreaM2", "building.floor", "building.yearBuilt", "availability.availableFrom"];

    private static readonly string[] DetailSections = ["size", "building", "energy", "location"];

    private static readonly string[] HiddenDetailPaths =
        ["location.street", "location.houseNumber", "location.zip", "location.city"];

    private const string Styles = """
        :root { --ink:#111418; --paper:#FFFFFF; --canvas:#F3F4F1; --well:#F6F7F4; --line:#E1E4DD; --muted:#5E6660;
                --lime:#B8E62E; --green:#4E8A1E; --green-soft:#E3F1D2; }
        * { box-sizing:border-box; }
        body { margin:0; background:var(--canvas); color:var(--ink); font:15px/1.55 Outfit, system-ui, sans-serif; }
        img { display:block; max-width:100%; }
        a { color:inherit; }
        .site { max-width:1120px; margin:0 auto; padding:18px 24px; display:flex; align-items:baseline; justify-content:space-between; }
        .brand { font-weight:700; font-size:20px; letter-spacing:-0.02em; text-decoration:none; }
        .brand span { color:#8FB61E; }
        .tagline { font-size:13px; color:var(--muted); }
        main { max-width:1120px; margin:0 auto; padding:0 24px 48px; }
        .gallery { display:grid; grid-template-columns:2fr 1fr 1fr; grid-template-rows:200px 200px; gap:4px; border-radius:8px; overflow:hidden; }
        .gallery a { position:relative; display:block; background:var(--well); }
        .gallery a:first-child { grid-row:span 2; }
        .gallery img { width:100%; height:100%; object-fit:cover; }
        .gallery .more { position:absolute; right:8px; bottom:8px; font-size:12px; padding:3px 9px; border-radius:4px; background:var(--paper); }
        .content { display:grid; grid-template-columns:minmax(0, 1.7fr) minmax(0, 1fr); gap:28px; margin-top:24px; align-items:start; }
        .address { margin:0; font-size:14px; color:var(--muted); }
        h1 { margin:2px 0 14px; font-size:26px; line-height:1.2; letter-spacing:-0.01em; }
        h2 { margin:28px 0 10px; font-size:15px; }
        .chips { display:flex; flex-wrap:wrap; gap:6px; }
        .chip { padding:5px 10px; border-radius:4px; background:var(--paper); font-size:14px; }
        .chip.feature { background:var(--green-soft); color:#2E5410; }
        .description { color:#333A35; white-space:pre-line; }
        .details { display:grid; grid-template-columns:repeat(2, minmax(0, 1fr)); gap:0 24px; margin:0; }
        .details div { display:flex; justify-content:space-between; gap:12px; padding:8px 0; border-bottom:1px solid var(--line); }
        .details dt { color:var(--muted); } .details dd { margin:0; font-weight:500; text-align:right; }
        .side { position:sticky; top:20px; background:var(--paper); border-radius:8px; padding:20px; }
        .price-label { font-size:13px; color:var(--muted); }
        .price { font-size:26px; font-weight:600; margin:0 0 12px; }
        .side-lines { display:grid; grid-template-columns:1fr auto; gap:4px 12px; font-size:14px; margin:0 0 16px; }
        .side-lines dt { color:var(--muted); } .side-lines dd { margin:0; }
        .inquiry { display:block; text-align:center; text-decoration:none; font-weight:600; padding:12px; border-radius:6px; background:var(--lime); }
        .side-note { margin:10px 0 0; font-size:12px; color:var(--muted); text-align:center; }
        .contact { display:grid; gap:4px; font-size:14px; }
        .all-images { display:grid; grid-template-columns:repeat(auto-fill, minmax(220px, 1fr)); gap:6px; }
        .all-images img { width:100%; border-radius:4px; background:var(--well); }
        footer { max-width:1120px; margin:0 auto; padding:24px; font-size:12px; color:var(--muted); }
        .message { max-width:520px; margin:18vh auto; padding:0 24px; text-align:center; }
        @media (max-width: 860px) {
            .gallery { grid-template-columns:1fr 1fr; grid-template-rows:240px 120px; }
            .gallery a:first-child { grid-column:span 2; grid-row:auto; }
            .gallery a:nth-child(n+4) { display:none; }
            .content { grid-template-columns:1fr; }
            .side { position:static; }
            .details { grid-template-columns:1fr; }
        }
        """;

    public static string Render(string token, ListingReview listing, string inquiryAddress)
    {
        JsonObject data = JsonNode.Parse(listing.DataJson) as JsonObject ?? [];
        string title = ListingValues.Text(data, "title") ?? "Inserat";

        StringBuilder html = new();
        AppendHead(html, title);
        html.Append("<main>");
        AppendGallery(html, token, listing.Images);
        html.Append("<div class=\"content\"><div>");
        AppendIntro(html, data, title);
        AppendFeatures(html, data);
        AppendDetails(html, data);
        html.Append("</div>");
        AppendSide(html, listing, data, title, inquiryAddress);
        html.Append("</div>");
        AppendAllImages(html, token, listing.Images);
        html.Append("</main><footer>Diese Seite ist nur über Ihren persönlichen Link erreichbar. Vermittelt durch Latrovu.</footer></body></html>");
        return html.ToString();
    }

    public static string RenderUnavailable()
    {
        StringBuilder html = new();
        AppendHead(html, "Inserat nicht verfügbar");
        return html.Append("<div class=\"message\"><h1>Dieser Link ist nicht mehr gültig</h1>")
            .Append("<p>Das Inserat wurde zurückgezogen oder der Link ist abgelaufen. ")
            .Append("Neue passende Wohnungen schicken wir Ihnen weiterhin direkt.</p></div></body></html>")
            .ToString();
    }

    private static void AppendHead(StringBuilder html, string title) =>
        html.Append(Head(title))
            .Append($"<header class=\"site\">{SiteAssets.LogoImage(28)}<span class=\"tagline\">Te la trovo.</span></header>");

    private static string Head(string title) =>
        "<!doctype html><html lang=\"de-CH\"><head><meta charset=\"utf-8\">"
        + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><meta name=\"robots\" content=\"noindex, nofollow\">"
        + $"<title>{Encode(title)} – Latrovu</title>{SiteAssets.HeadLinks}<style>{SiteAssets.FontFaces}{Styles}</style></head><body>";

    private static void AppendGallery(StringBuilder html, string token, IReadOnlyList<ListingImageRef> images)
    {
        if (images.Count == 0)
        {
            return;
        }

        html.Append("<section class=\"gallery\" aria-label=\"Bilder\">");
        List<ListingImageRef> preview = images.Take(GalleryPreviewCount).ToList();
        for (int index = 0; index < preview.Count; index++)
        {
            string more = index == preview.Count - 1 && images.Count > preview.Count
                ? $"<span class=\"more\">Alle {images.Count} Bilder</span>"
                : string.Empty;
            html.Append($"<a href=\"#bilder\"><img src=\"{ImageUrl(token, preview[index])}\" alt=\"Bild {index + 1}\">{more}</a>");
        }

        html.Append("</section>");
    }

    private static void AppendIntro(StringBuilder html, JsonObject data, string title)
    {
        html.Append($"<p class=\"address\">{Encode(ListingValues.Address(data))}</p><h1>{Encode(title)}</h1><div class=\"chips\">");
        foreach (string path in HeadlinePaths)
        {
            string? text = Display(data, path);
            if (text is not null)
            {
                html.Append($"<span class=\"chip\">{Encode(HeadlineText(path, text))}</span>");
            }
        }

        html.Append("</div>");
        string? description = ListingValues.Text(data, "description.summary") ?? ListingValues.Text(data, "description.original");
        if (description is not null)
        {
            html.Append($"<h2>Beschreibung</h2><p class=\"description\">{Encode(description)}</p>");
        }
    }

    private static void AppendFeatures(StringBuilder html, JsonObject data)
    {
        List<string> features = ListingFieldCatalog.Fields
            .Where(field => field.Kind == FieldKind.Boolean && field.Section == "features"
                && JsonPaths.Get(data, field.Path) is JsonValue value && value.TryGetValue(out bool isSet) && isSet)
            .Select(field => FieldLabels.Label(field.Path))
            .ToList();
        if (features.Count == 0)
        {
            return;
        }

        html.Append("<h2>Ausstattung</h2><div class=\"chips\">")
            .AppendJoin(string.Empty, features.Select(feature => $"<span class=\"chip feature\">✓ {Encode(feature)}</span>"))
            .Append("</div>");
    }

    private static void AppendDetails(StringBuilder html, JsonObject data)
    {
        List<(string Label, string Value)> details = ListingFieldCatalog.Fields
            .Where(field => DetailSections.Contains(field.Section) && field.Kind != FieldKind.Boolean
                && !HeadlinePaths.Contains(field.Path) && !HiddenDetailPaths.Contains(field.Path))
            .Select(field => (FieldLabels.Label(field.Path), Display(data, field.Path)))
            .Where(detail => detail.Item2 is not null)
            .Select(detail => (detail.Item1, detail.Item2!))
            .ToList();
        if (details.Count == 0)
        {
            return;
        }

        html.Append("<h2>Details</h2><dl class=\"details\">")
            .AppendJoin(string.Empty, details.Select(detail => $"<div><dt>{Encode(detail.Label)}</dt><dd>{Encode(detail.Value)}</dd></div>"))
            .Append("</dl>");
    }

    private static void AppendSide(StringBuilder html, ListingReview listing, JsonObject data, string title, string inquiryAddress)
    {
        (string label, string? price) = Display(data, "sale.priceChf") is { } salePrice
            ? ("Kaufpreis", salePrice)
            : Display(data, "rent.grossChf") is { } grossRent ? ("Miete brutto pro Monat", grossRent) : ("Nettomiete pro Monat", Display(data, "rent.netChf"));

        html.Append("<aside class=\"side\">");
        if (price is not null)
        {
            html.Append($"<div class=\"price-label\">{Encode(label)}</div><p class=\"price\">{Encode(price)}</p>");
        }

        AppendSideLines(html, data);
        AppendContact(html, listing, data, title, inquiryAddress);
        html.Append("</aside>");
    }

    private static void AppendSideLines(StringBuilder html, JsonObject data)
    {
        string[] paths = ["rent.chargesChf", "rent.depositChf", "sale.condoFeesChfPerYear"];
        List<string> lines = paths
            .Select(path => (Label: FieldLabels.Label(path), Value: Display(data, path)))
            .Where(line => line.Value is not null)
            .Select(line => $"<dt>{Encode(line.Label)}</dt><dd>{Encode(line.Value!)}</dd>")
            .ToList();
        if (lines.Count > 0)
        {
            html.Append("<dl class=\"side-lines\">").AppendJoin(string.Empty, lines).Append("</dl>");
        }
    }

    private static void AppendContact(StringBuilder html, ListingReview listing, JsonObject data, string title, string inquiryAddress)
    {
        if (listing.PublishContact)
        {
            AppendPublicContact(html, data);
            return;
        }

        string subject = Uri.EscapeDataString($"Anfrage: {title}");
        html.Append($"<a class=\"inquiry\" href=\"mailto:{Encode(inquiryAddress)}?subject={subject}\">Anfrage senden</a>")
            .Append("<p class=\"side-note\">Ihre Anfrage geht über Latrovu an den Anbieter.</p>");
    }

    private static void AppendPublicContact(StringBuilder html, JsonObject data)
    {
        html.Append("<div class=\"contact\">");
        foreach (string path in new[] { "contact.company", "contact.name" })
        {
            if (ListingValues.Text(data, path) is { } text)
            {
                html.Append($"<span>{Encode(text)}</span>");
            }
        }

        if (ListingValues.Text(data, "contact.phone") is { } phone)
        {
            html.Append($"<a href=\"tel:{Encode(phone.Replace(" ", string.Empty, StringComparison.Ordinal))}\">{Encode(phone)}</a>");
        }

        if (ListingValues.Text(data, "contact.email") is { } email)
        {
            html.Append($"<a href=\"mailto:{Encode(email)}\">{Encode(email)}</a>");
        }

        html.Append("</div>");
    }

    private static void AppendAllImages(StringBuilder html, string token, IReadOnlyList<ListingImageRef> images)
    {
        if (images.Count <= GalleryPreviewCount)
        {
            return;
        }

        html.Append($"<h2 id=\"bilder\">Alle {images.Count} Bilder</h2><section class=\"all-images\">")
            .AppendJoin(string.Empty, images.Select(image => $"<img loading=\"lazy\" src=\"{ImageUrl(token, image)}\" alt=\"Bild {image.Position}\">"))
            .Append("</section>");
    }

    private static string? Display(JsonObject data, string path)
    {
        FieldDefinition? field = ListingFieldCatalog.Find(path);
        JsonNode? value = JsonPaths.Get(data, path);
        return field is null || !ReviewFieldRenderer.HasValue(value) ? null : ReviewFieldRenderer.DisplayText(field, value);
    }

    private static string HeadlineText(string path, string text) => path switch
    {
        "size.rooms" => $"{text} Zimmer",
        "building.floor" => $"{text}. Etage",
        "building.yearBuilt" => $"Baujahr {text}",
        "availability.availableFrom" => $"Bezug {text}",
        _ => text
    };

    private static string ImageUrl(string token, ListingImageRef image) => $"/i/{Encode(token)}/images/{image.Position}";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
