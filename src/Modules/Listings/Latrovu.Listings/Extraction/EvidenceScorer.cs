using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Latrovu.Listings.Extraction;

public static partial class EvidenceScorer
{
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

    private static bool ValueAppearsInText(string normalizedText, string path, JsonNode value) =>
        !ListingKeyFields.InferredPaths.Contains(path)
        && EvidenceNeedles.FromValue(value).Any(needle => normalizedText.Contains(Normalize(needle), StringComparison.Ordinal));

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
