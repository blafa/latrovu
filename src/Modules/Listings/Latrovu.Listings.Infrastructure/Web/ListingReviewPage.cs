using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Latrovu.Listings.Infrastructure.Web;

public static class ListingReviewPage
{
    private const string PageTitle = "Inserat prüfen";
    private const string ListSeparator = ", ";
    private const string NoScore = "none";

    private static readonly Dictionary<string, string> SectionLabels = new()
    {
        [ListingFieldCatalog.GeneralSection] = "Allgemein",
        ["location"] = "Lage",
        ["size"] = "Grösse",
        ["building"] = "Gebäude",
        ["rent"] = "Miete",
        ["sale"] = "Verkauf",
        ["parking"] = "Parkplätze",
        ["features"] = "Ausstattung",
        ["energy"] = "Energie",
        ["availability"] = "Verfügbarkeit",
        ["viewing"] = "Besichtigung",
        ["application"] = "Bewerbung",
        ["contact"] = "Kontakt",
        ["description"] = "Beschreibung",
        ["documents"] = "Dokumente",
        ["otherDetails"] = "Weitere Angaben"
    };

    public static string Render(string token, ListingReview review)
    {
        ArgumentNullException.ThrowIfNull(review);

        JsonObject data = JsonNode.Parse(review.DataJson) as JsonObject ?? [];
        JsonObject scores = JsonNode.Parse(review.FieldScoresJson) as JsonObject ?? [];
        StringBuilder html = new();
        ReviewLayout.AppendHead(html, PageTitle);
        AppendHeader(html, token, review);
        AppendImages(html, token, review.Images);
        foreach (IGrouping<string, FieldDefinition> section in ListingFieldCatalog.Fields.GroupBy(field => field.Section))
        {
            AppendSection(html, token, section, data, scores);
        }

        ReviewLayout.AppendEnd(html);
        return html.ToString();
    }

    public static string RenderFieldStatus(FieldUpdateResult result) => result switch
    {
        FieldUpdateResult.Saved => "gespeichert ✓",
        FieldUpdateResult.InvalidValue => "ungültiger Wert",
        FieldUpdateResult.UnknownField => "unbekanntes Feld",
        _ => "nicht gefunden"
    };

    public static string RenderApproveStatus(bool approved) =>
        approved ? "Freigegeben ✓" : "Freigabe nicht möglich (bereits freigegeben oder nicht gefunden)";

    private static void AppendHeader(StringBuilder html, string token, ListingReview review)
    {
        string fileName = WebUtility.HtmlEncode(review.FileName ?? string.Empty);
        html.Append("<header><div><h1>Inserat prüfen</h1>")
            .Append($"<div class=\"muted\">{fileName} · Status: {WebUtility.HtmlEncode(review.Status)}</div></div>")
            .Append($"<div><button hx-post=\"/review/{Encode(token)}/approve\" hx-target=\"#approve-status\">Freigeben</button>")
            .Append("<div id=\"approve-status\" class=\"status\"></div></div></header>");
    }

    private static void AppendImages(StringBuilder html, string token, IReadOnlyList<ListingImageRef> images)
    {
        if (images.Count == 0)
        {
            return;
        }

        html.Append($"<h2>Bilder ({images.Count})</h2><div class=\"images\">");
        foreach (ListingImageRef image in images)
        {
            html.Append($"<img loading=\"lazy\" alt=\"Bild {image.Position}\" src=\"/review/{Encode(token)}/images/{image.Position}\">");
        }

        html.Append("</div>");
    }

    private static void AppendSection(
        StringBuilder html,
        string token,
        IGrouping<string, FieldDefinition> section,
        JsonObject data,
        JsonObject scores)
    {
        string label = SectionLabels.GetValueOrDefault(section.Key, section.Key);
        html.Append($"<h2>{WebUtility.HtmlEncode(label)}</h2>");
        foreach (FieldDefinition field in section)
        {
            AppendField(html, token, field, JsonPaths.Get(data, field.Path), ScoreOf(scores, field.Path));
        }
    }

    private static void AppendField(StringBuilder html, string token, FieldDefinition field, JsonNode? value, string score)
    {
        string statusId = $"status-{field.Path.Replace('.', '-')}";
        string label = field.Path.Split('.')[^1];
        string request = $"hx-post=\"/review/{Encode(token)}/fields\" hx-trigger=\"change\" hx-target=\"#{statusId}\" "
            + $"hx-vals='{{\"path\":\"{Encode(field.Path)}\"}}' name=\"value\"";

        html.Append($"<div class=\"field score-{score}\"><label title=\"{Encode(field.Path)}\">{Encode(label)}</label>")
            .Append(RenderInput(field, value, request))
            .Append($"<span id=\"{statusId}\" class=\"status\"></span></div>");
    }

    private static string RenderInput(FieldDefinition field, JsonNode? value, string request)
    {
        string current = Encode(DisplayValue(value, field.Kind));
        return field.Kind switch
        {
            FieldKind.Boolean => RenderSelect(request, current, [("true", "Ja"), ("false", "Nein")]),
            FieldKind.Choice => RenderSelect(request, current, field.Choices.Select(choice => (choice, choice)).ToList()),
            FieldKind.Json => $"<textarea rows=\"3\" {request}>{current}</textarea>",
            _ => $"<input type=\"text\" value=\"{current}\" {request}>"
        };
    }

    private static string RenderSelect(string request, string current, IReadOnlyList<(string Value, string Label)> options)
    {
        StringBuilder select = new($"<select {request}><option value=\"\">–</option>");
        foreach ((string optionValue, string optionLabel) in options)
        {
            string selected = optionValue == current ? " selected" : string.Empty;
            select.Append($"<option value=\"{Encode(optionValue)}\"{selected}>{Encode(optionLabel)}</option>");
        }

        return select.Append("</select>").ToString();
    }

    private static string DisplayValue(JsonNode? value, FieldKind kind)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (kind == FieldKind.TextList && value is JsonArray items)
        {
            return string.Join(ListSeparator, items.Select(item => item?.ToString()));
        }

        return value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : value.ToJsonString();
    }

    private static string ScoreOf(JsonObject scores, string path) =>
        scores[path] is JsonValue score && score.TryGetValue(out string? name) ? name : NoScore;

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
