using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Latrovu.Listings.Infrastructure.Review.Web;

internal static class ReviewSummary
{
    public const string ImagesPane = "images";
    private const string SuggestionListId = "field-suggestions";
    private const string TitlePath = "title";
    private const string ContactSection = "contact";
    private const string FoundMark = "found";
    private const string ConfirmedMark = "confirmed";
    private const string NoSourceMark = "none";
    private const string OpenMark = "open";
    private const string YesChip = "yes";
    private const string NoChip = "no";

    public static void Append(StringBuilder html, ReviewModel model)
    {
        html.Append("<section class=\"panel summary\" aria-label=\"Übernommene Angaben\">");
        AppendHeader(html, model);
        AppendOpenPoints(html, model);
        foreach (ReviewGroup group in ReviewGroups.All)
        {
            AppendGroup(html, model, group);
        }

        AppendAddDetail(html, model);
        html.Append("</section>");
    }

    public static string SavedValueOob(FieldDefinition field, JsonNode? value)
    {
        string id = ReviewFieldRenderer.InputId(field);
        return field.Kind == FieldKind.Boolean
            ? Chip(field, value, oob: true)
            : $"<span class=\"spec-value\" id=\"v-{id}\" hx-swap-oob=\"true\">{Encode(ReviewFieldRenderer.DisplayText(field, value))}</span>";
    }

    private static void AppendHeader(StringBuilder html, ReviewModel model)
    {
        int total = model.Filled.Count;
        int open = model.Open.Count;
        int confirmed = model.Filled.Count(model.IsConfirmed);
        int found = model.Filled.Count(field => !model.IsConfirmed(field) && !model.Open.Contains(field) && model.EvidenceOf(field) is not null);
        int withoutSource = total - open - confirmed - found;
        string openText = open == 0 ? "Nichts zu prüfen." : $"{open} {(open == 1 ? "Angabe" : "Angaben")} bitte kurz prüfen.";

        html.Append("<header class=\"summary-header\"><div>")
            .Append($"<h2>{total} Angaben aus Ihrem Exposé übernommen</h2>")
            .Append($"<p>{openText}</p><ul class=\"legend\">")
            .Append(LegendItem(FoundMark, found, "im Exposé gefunden"))
            .Append(LegendItem(ConfirmedMark, confirmed, "von Ihnen bestätigt"))
            .Append(LegendItem(NoSourceMark, withoutSource, "ohne wörtliche Fundstelle"))
            .Append(LegendItem(OpenMark, open, "bitte prüfen"))
            .Append("</ul></div>");
        if (model.Review.Images.Count > 0)
        {
            html.Append($"<button type=\"button\" class=\"ghost\" data-select=\"{ImagesPane}\">")
                .Append($"{ReviewIcons.Photo}{model.Review.Images.Count} Bilder ansehen</button>");
        }

        html.Append("</header>");
    }

    private static string LegendItem(string kind, int count, string label) =>
        count == 0 ? string.Empty : $"<li>{MarkHtml(kind, null)}{count} {Encode(label)}</li>";

    private static void AppendOpenPoints(StringBuilder html, ReviewModel model)
    {
        if (model.Open.Count == 0)
        {
            return;
        }

        html.Append("<section class=\"open-points\" aria-label=\"Offene Angaben\"><h3>Bitte prüfen</h3>");
        foreach (FieldDefinition field in model.Open)
        {
            string id = ReviewFieldRenderer.InputId(field);
            string reason = model.EvidenceOf(field) is null ? "Im Exposé nicht gefunden" : "Weicht vom Exposé ab";
            html.Append("<div class=\"open-row\">")
                .Append(Spec(model, field, reason))
                .Append($"<button type=\"button\" class=\"ghost\" hx-post=\"/review/{Encode(model.Token)}/confirm\" ")
                .Append($"hx-vals='{{\"path\":\"{Encode(field.Path)}\"}}' hx-swap=\"none\">{ReviewIcons.Confirm}Stimmt</button>")
                .Append($"<button type=\"button\" class=\"ghost\" data-select=\"{id}\">Ändern</button></div>");
        }

        html.Append("</section>");
    }

    private static void AppendGroup(StringBuilder html, ReviewModel model, ReviewGroup group)
    {
        List<FieldDefinition> fields = model.Filled
            .Where(field => group.Sections.Contains(field.Section) && !model.Open.Contains(field))
            .ToList();
        bool isContactGroup = group.Sections.Contains(ContactSection);
        if (fields.Count == 0 && !isContactGroup)
        {
            return;
        }

        html.Append($"<section class=\"group\"><h3>{group.Icon}{Encode(group.Label)}</h3>");
        if (isContactGroup)
        {
            AppendPublishContactToggle(html, model);
        }

        AppendChips(html, model, fields.Where(field => field.Kind == FieldKind.Boolean).ToList());
        AppendSpecs(html, model, fields.Where(field => field.Kind != FieldKind.Boolean).ToList());
        html.Append("</section>");
    }

    private static void AppendChips(StringBuilder html, ReviewModel model, List<FieldDefinition> fields)
    {
        if (fields.Count == 0)
        {
            return;
        }

        html.Append("<div class=\"chips\">")
            .AppendJoin(string.Empty, fields.Select(field => Chip(field, model.ValueOf(field), oob: false)))
            .Append("</div>");
    }

    private static void AppendSpecs(StringBuilder html, ReviewModel model, List<FieldDefinition> fields)
    {
        if (fields.Count == 0)
        {
            return;
        }

        html.Append("<div class=\"specs\">");
        foreach (FieldDefinition field in fields.OrderByDescending(IsFullWidth))
        {
            html.Append(Spec(model, field, null));
        }

        html.Append("</div>");
    }

    private static string Chip(FieldDefinition field, JsonNode? value, bool oob)
    {
        string id = ReviewFieldRenderer.InputId(field);
        bool isYes = value?.GetValueKind() == JsonValueKind.True;
        string state = isYes ? YesChip : NoChip;
        string swap = oob ? " hx-swap-oob=\"true\"" : string.Empty;
        string label = Encode(FieldLabels.Label(field.Path));
        string title = isYes ? label : $"Kein(e) {label}";

        return $"<button type=\"button\" class=\"chip {state}\" id=\"v-{id}\" data-select=\"{id}\" title=\"{title}\"{swap}>"
            + $"{(isYes ? ReviewIcons.Confirm : ReviewIcons.Cross)}<span>{label}</span></button>";
    }

    private static void AppendPublishContactToggle(StringBuilder html, ReviewModel model)
    {
        string isChecked = model.Review.PublishContact ? " checked" : string.Empty;
        html.Append("<div class=\"publish-toggle\"><label class=\"switch\">")
            .Append($"<input type=\"checkbox\" name=\"value\" value=\"true\" hx-post=\"/review/{Encode(model.Token)}/publish-contact\" ")
            .Append($"hx-trigger=\"change\" hx-target=\"#publish-contact-status\"{isChecked}>")
            .Append("<span class=\"switch-track\" aria-hidden=\"true\"></span>")
            .Append($"<span class=\"switch-icon on\">{ReviewIcons.Eye}</span><span class=\"switch-icon off\">{ReviewIcons.EyeOff}</span>")
            .Append("<span>Kontaktdaten im öffentlichen Inserat anzeigen</span></label>")
            .Append("<span id=\"publish-contact-status\" class=\"status\" aria-live=\"polite\"></span>")
            .Append("<p class=\"toggle-hint\">Standardmässig aus: Interessierte erreichen Sie über Latrovu, ")
            .Append("Ihre E-Mail-Adresse und Telefonnummer bleiben verborgen.</p></div>");
    }

    private static bool IsFullWidth(FieldDefinition field) =>
        field.Path == TitlePath || FieldLabels.LongTextPaths.Contains(field.Path);

    private static string Spec(ReviewModel model, FieldDefinition field, string? reason)
    {
        string id = ReviewFieldRenderer.InputId(field);
        string wide = IsFullWidth(field) ? " wide" : string.Empty;
        string display = ReviewFieldRenderer.DisplayText(field, model.ValueOf(field));
        string value = reason is null
            ? Encode(display)
            : $"{Encode(display)} <span class=\"spec-note\">{Encode(reason)}</span>";

        return $"<button type=\"button\" class=\"spec{wide}\" data-select=\"{id}\">"
            + $"<span class=\"spec-label\">{Encode(FieldLabels.Label(field.Path))}{Mark(model, field, reason is not null)}</span>"
            + $"<span class=\"spec-value\" id=\"v-{id}\">{value}</span></button>";
    }

    private static string Mark(ReviewModel model, FieldDefinition field, bool open)
    {
        if (open)
        {
            return MarkHtml(OpenMark, "Bitte prüfen");
        }

        if (model.IsConfirmed(field))
        {
            return MarkHtml(ConfirmedMark, "Von Ihnen bestätigt");
        }

        return model.EvidenceOf(field) is null
            ? MarkHtml(NoSourceMark, "Ohne wörtliche Fundstelle")
            : MarkHtml(FoundMark, "Im Exposé gefunden");
    }

    private static string MarkHtml(string kind, string? title)
    {
        string icon = kind is FoundMark or ConfirmedMark ? ReviewIcons.Check : string.Empty;
        string tooltip = title is null ? string.Empty : $" title=\"{Encode(title)}\"";
        return $"<span class=\"mark {kind}\"{tooltip}>{icon}</span>";
    }

    private static void AppendAddDetail(StringBuilder html, ReviewModel model)
    {
        html.Append($"<form class=\"add-detail\" hx-post=\"/review/{Encode(model.Token)}/details\" hx-target=\"find .status\">")
            .Append("<span class=\"add-title\">Angabe ergänzen</span>")
            .Append($"<input name=\"label\" list=\"{SuggestionListId}\" placeholder=\"Bezeichnung, z.B. Kaution\" aria-label=\"Bezeichnung\" required>")
            .Append("<input name=\"value\" placeholder=\"Wert, z.B. 3 Monatsmieten\" aria-label=\"Wert\" required>")
            .Append($"<button type=\"submit\" class=\"ghost\">{ReviewIcons.Plus}Hinzufügen</button><span class=\"status\" aria-live=\"polite\"></span>")
            .Append($"<datalist id=\"{SuggestionListId}\">");
        foreach (FieldDefinition field in model.Empty.Where(field => field.Kind is not (FieldKind.ObjectList or FieldKind.Json)))
        {
            html.Append($"<option value=\"{Encode(FieldLabels.Label(field.Path))}\"></option>");
        }

        html.Append("</datalist></form>");
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
