using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Latrovu.Listings.Infrastructure.Review.Web;

internal static class ObjectListRenderer
{
    private const string NumberKind = "number";
    private const string BooleanKind = "boolean";
    private const string TextKind = "text";
    private const string TrueValue = "true";
    private const string FalseValue = "false";

    public static string Render(FieldDefinition field, JsonNode? value, string request)
    {
        JsonArray items = value as JsonArray ?? [];
        StringBuilder html = new("<div class=\"object-list\" data-list>");
        html.Append($"<input type=\"hidden\" value=\"{Encode(items.ToJsonString())}\" {request}>")
            .Append("<table><thead><tr>");
        foreach (FieldDefinition itemField in field.ItemFields)
        {
            html.Append($"<th scope=\"col\">{Encode(FieldLabels.Label(itemField.Path))}</th>");
        }

        html.Append("<th scope=\"col\"><span class=\"visually-hidden\">Entfernen</span></th></tr></thead><tbody>");
        foreach (JsonObject item in items.OfType<JsonObject>())
        {
            html.Append(Row(field, item));
        }

        return html.Append($"</tbody></table><template>{Row(field, null)}</template>")
            .Append("<button type=\"button\" class=\"secondary\" data-add-row>Zeile hinzufügen</button></div>")
            .ToString();
    }

    private static string Row(FieldDefinition field, JsonObject? item)
    {
        StringBuilder row = new("<tr>");
        foreach (FieldDefinition itemField in field.ItemFields)
        {
            row.Append($"<td>{Cell(itemField, item?[itemField.Key])}</td>");
        }

        return row.Append("<td><button type=\"button\" class=\"remove\" data-remove-row aria-label=\"Zeile entfernen\">×</button></td></tr>")
            .ToString();
    }

    private static string Cell(FieldDefinition itemField, JsonNode? value)
    {
        string current = value is null ? string.Empty
            : value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : value.ToJsonString();
        string attributes = $"data-key=\"{Encode(itemField.Key)}\" aria-label=\"{Encode(FieldLabels.Label(itemField.Path))}\"";

        return itemField.Kind switch
        {
            FieldKind.Number => $"<input type=\"text\" inputmode=\"decimal\" data-kind=\"{NumberKind}\" {attributes} value=\"{Encode(current)}\">",
            FieldKind.Boolean => Select(attributes, BooleanKind, current, [(TrueValue, "Ja"), (FalseValue, "Nein")]),
            FieldKind.Choice => Select(attributes, TextKind, current, itemField.Choices.Select(choice => (choice, FieldLabels.ChoiceName(choice))).ToList()),
            _ => $"<input type=\"text\" data-kind=\"{TextKind}\" {attributes} value=\"{Encode(current)}\">"
        };
    }

    private static string Select(string attributes, string kind, string current, IReadOnlyList<(string Value, string Label)> options)
    {
        StringBuilder select = new($"<select data-kind=\"{kind}\" {attributes}><option value=\"\">–</option>");
        foreach ((string optionValue, string optionLabel) in options)
        {
            string selected = optionValue == current ? " selected" : string.Empty;
            select.Append($"<option value=\"{Encode(optionValue)}\"{selected}>{Encode(optionLabel)}</option>");
        }

        return select.Append("</select>").ToString();
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
