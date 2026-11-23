using System.Globalization;
using System.Text.Json.Nodes;

namespace Latrovu.Listings;

public static class FieldValueParser
{
    private const char ListSeparator = ',';
    private const string TrueValue = "true";
    private const string FalseValue = "false";
    private static readonly string[] IgnoredNumberCharacters = ["'", "\u2019", " "];

    public static JsonNode? Parse(FieldDefinition field, string? raw)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        string value = raw.Trim();
        return field.Kind switch
        {
            FieldKind.Number => ParseNumber(value),
            FieldKind.Boolean => ParseBoolean(value),
            FieldKind.Choice => ParseChoice(field, value),
            FieldKind.TextList => ParseTextList(value),
            FieldKind.Json => JsonNode.Parse(value),
            _ => JsonValue.Create(value)
        };
    }

    private static JsonValue ParseNumber(string value)
    {
        string cleaned = IgnoredNumberCharacters.Aggregate(value, (current, character) => current.Replace(character, string.Empty));
        if (!decimal.TryParse(cleaned.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number))
        {
            throw new FormatException($"'{value}' is not a number");
        }

        return JsonValue.Create(number);
    }

    private static JsonValue ParseBoolean(string value) => value switch
    {
        TrueValue => JsonValue.Create(true),
        FalseValue => JsonValue.Create(false),
        _ => throw new FormatException($"'{value}' is not a yes/no value")
    };

    private static JsonValue ParseChoice(FieldDefinition field, string value) =>
        field.Choices.Contains(value)
            ? JsonValue.Create(value)
            : throw new FormatException($"'{value}' is not an allowed value for {field.Path}");

    private static JsonArray ParseTextList(string value)
    {
        JsonArray items = [];
        foreach (string item in value.Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            items.Add(item);
        }

        return items;
    }
}
