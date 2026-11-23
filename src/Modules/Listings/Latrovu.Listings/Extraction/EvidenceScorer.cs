using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Latrovu.Listings.Extraction;

public static partial class EvidenceScorer
{
    private const decimal MinMatchableNumber = 100;
    private const int MinMatchableTextLength = 3;
    private const string GroupedNumberFormat = "#,0";
    private const string DecimalNumberFormat = "0.0#";
    private const string PlainNumberFormat = "0";
    private static readonly string[] ThousandsSeparators = ["'", " ", "."];

    public static IReadOnlyDictionary<string, FieldConfidence> Score(
        string documentText,
        JsonObject data,
        IEnumerable<FieldEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(documentText);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(evidence);

        string normalizedText = Normalize(documentText);
        Dictionary<string, string> quotesByField = evidence
            .GroupBy(item => item.Field)
            .ToDictionary(group => group.Key, group => group.First().Quote);

        Dictionary<string, FieldConfidence> scores = [];
        foreach (string path in ListingKeyFields.Paths)
        {
            JsonNode? value = FindValue(data, path);
            if (value is not null)
            {
                scores[path] = ScoreField(normalizedText, path, value, quotesByField.GetValueOrDefault(path));
            }
        }

        return scores;
    }

    public static string Normalize(string text)
    {
        string unified = text
            .Replace('\u2019', '\'')
            .Replace('\u2018', '\'')
            .Replace('\u2013', '-')
            .Replace('\u2014', '-')
            .Replace('\u00A0', ' ');
        return Whitespace().Replace(unified, " ").Trim().ToLowerInvariant();
    }

    private static FieldConfidence ScoreField(string normalizedText, string path, JsonNode value, string? quote)
    {
        bool quoteFound = !string.IsNullOrWhiteSpace(quote)
            && normalizedText.Contains(Normalize(quote), StringComparison.Ordinal);
        if (quoteFound || ValueAppearsInText(normalizedText, path, value))
        {
            return FieldConfidence.High;
        }

        return string.IsNullOrWhiteSpace(quote) ? FieldConfidence.Low : FieldConfidence.Medium;
    }

    private static bool ValueAppearsInText(string normalizedText, string path, JsonNode value)
    {
        if (ListingKeyFields.InferredPaths.Contains(path))
        {
            return false;
        }

        return value.GetValueKind() switch
        {
            JsonValueKind.Number => NumberVariants(value.GetValue<decimal>())
                .Any(variant => normalizedText.Contains(variant, StringComparison.Ordinal)),
            JsonValueKind.String => TextAppears(normalizedText, value.GetValue<string>()),
            _ => false
        };
    }

    private static bool TextAppears(string normalizedText, string text)
    {
        string normalized = Normalize(text);
        return normalized.Length >= MinMatchableTextLength && normalizedText.Contains(normalized, StringComparison.Ordinal);
    }

    private static IEnumerable<string> NumberVariants(decimal number)
    {
        if (number < MinMatchableNumber)
        {
            return [];
        }

        if (number != decimal.Truncate(number))
        {
            string withPoint = number.ToString(DecimalNumberFormat, CultureInfo.InvariantCulture);
            return [withPoint, withPoint.Replace('.', ',')];
        }

        string grouped = number.ToString(GroupedNumberFormat, CultureInfo.InvariantCulture);
        return ThousandsSeparators
            .Select(separator => grouped.Replace(",", separator, StringComparison.Ordinal))
            .Append(number.ToString(PlainNumberFormat, CultureInfo.InvariantCulture));
    }

    private static JsonNode? FindValue(JsonObject data, string path)
    {
        JsonNode? node = JsonPaths.Get(data, path);
        if (node is null)
        {
            return null;
        }

        bool isBlankText = node.GetValueKind() == JsonValueKind.String && string.IsNullOrWhiteSpace(node.GetValue<string>());
        return isBlankText ? null : node;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
