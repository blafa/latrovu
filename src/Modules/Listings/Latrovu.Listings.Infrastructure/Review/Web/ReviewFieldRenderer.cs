using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Latrovu.Listings.Infrastructure.Review.Web;

internal static class ReviewFieldRenderer
{
    public const string LowScore = "low";
    public const string MediumScore = "medium";
    private const string ListSeparator = ", ";
    private const string TrueValue = "true";
    private const string FalseValue = "false";
    private const string EmptyDisplay = "–";
    private const string Francs = "CHF";
    private const string NumberFormat = "#,0.##";
    private const string PlainNumberFormat = "0.##";
    private static readonly CultureInfo SwissCulture = CultureInfo.GetCultureInfo("de-CH");

    public static string InputId(FieldDefinition field) => $"f-{field.Path.Replace('.', '-')}";

    public static bool HasValue(JsonNode? value) =>
        value is not null
        && !(value.GetValueKind() == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetValue<string>()))
        && !(value is JsonArray items && items.Count == 0);

    public static bool IsWide(FieldDefinition field) =>
        FieldLabels.LongTextPaths.Contains(field.Path) || field.Kind is FieldKind.Json or FieldKind.ObjectList;

    public static string RenderEditor(string token, FieldDefinition field, JsonNode? value)
    {
        string id = InputId(field);
        string? unit = FieldLabels.Unit(field.Path);
        string request = $"hx-post=\"/review/{Encode(token)}/fields\" hx-trigger=\"change\" hx-target=\"#{id}-status\" "
            + $"hx-vals='{{\"path\":\"{Encode(field.Path)}\"}}' name=\"value\" id=\"{id}\"";

        return new StringBuilder("<div class=\"editor-field\">")
            .Append($"<div class=\"control\">{RenderInput(field, value, request)}")
            .Append(unit is null ? string.Empty : $"<span class=\"unit\">{Encode(unit)}</span>")
            .Append($"</div><span id=\"{id}-status\" class=\"status\" aria-live=\"polite\"></span></div>")
            .ToString();
    }

    public static string DisplayText(FieldDefinition field, JsonNode? value)
    {
        if (!HasValue(value))
        {
            return EmptyDisplay;
        }

        return field.Kind switch
        {
            FieldKind.Boolean => value!.GetValueKind() == JsonValueKind.True ? "Ja" : "Nein",
            FieldKind.Choice => FieldLabels.ChoiceName(value!.ToString()),
            FieldKind.Number => DisplayNumber(field, value!),
            FieldKind.TextList => string.Join(ListSeparator, value!.AsArray().Select(item => item?.ToString())),
            FieldKind.ObjectList or FieldKind.Json => DisplayEntries(value!),
            _ => value!.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : value.ToJsonString()
        };
    }

    private static string DisplayNumber(FieldDefinition field, JsonNode value)
    {
        string? unit = FieldLabels.Unit(field.Path);
        string format = unit is null ? PlainNumberFormat : NumberFormat;
        string number = value.GetValueKind() == JsonValueKind.Number
            ? value.GetValue<decimal>().ToString(format, SwissCulture)
            : value.ToString();
        return unit switch
        {
            null => number,
            Francs => $"{Francs} {number}",
            _ => $"{number} {unit}"
        };
    }

    private static string DisplayEntries(JsonNode value) =>
        value is JsonArray items ? $"{items.Count} {(items.Count == 1 ? "Eintrag" : "Einträge")}" : value.ToJsonString();

    private static string RenderInput(FieldDefinition field, JsonNode? value, string request)
    {
        string current = InputValue(value, field.Kind);
        if (field.Kind == FieldKind.ObjectList)
        {
            return ObjectListRenderer.Render(field, value, request);
        }

        if (IsWide(field))
        {
            return $"<textarea {request}>{Encode(current)}</textarea>";
        }

        return field.Kind switch
        {
            FieldKind.Boolean => RenderSelect(request, current, [(TrueValue, "Ja"), (FalseValue, "Nein")]),
            FieldKind.Choice => RenderSelect(
                request,
                current,
                field.Choices.Select(choice => (choice, FieldLabels.ChoiceName(choice))).ToList()),
            FieldKind.Number => $"<input type=\"text\" inputmode=\"decimal\" value=\"{Encode(current)}\" {request}>",
            _ => $"<input type=\"text\" value=\"{Encode(current)}\" {request}>"
        };
    }

    private static string RenderSelect(string request, string current, IReadOnlyList<(string Value, string Label)> options)
    {
        StringBuilder select = new($"<select {request}><option value=\"\">Keine Angabe</option>");
        foreach ((string optionValue, string optionLabel) in options)
        {
            string selected = optionValue == current ? " selected" : string.Empty;
            select.Append($"<option value=\"{Encode(optionValue)}\"{selected}>{Encode(optionLabel)}</option>");
        }

        return select.Append("</select>").ToString();
    }

    private static string InputValue(JsonNode? value, FieldKind kind)
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

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
