using System.Text.Json;
using System.Text.Json.Nodes;

namespace Latrovu.Listings;

public static class ListingExtractionBuilder
{
    private const string EvidenceProperty = "evidence";
    private const string EmptyScores = "{}";

    public static ListingExtraction Build(string extractedJson, string documentText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extractedJson);
        ArgumentNullException.ThrowIfNull(documentText);

        if (JsonNode.Parse(extractedJson) is not JsonObject data)
        {
            return new ListingExtraction(extractedJson, extractedJson, EmptyScores);
        }

        IReadOnlyList<FieldEvidence> evidence = ReadEvidence(data[EvidenceProperty]);
        data.Remove(EvidenceProperty);

        IReadOnlyDictionary<string, FieldConfidence> scores = EvidenceScorer.Score(documentText, data, evidence);
        Dictionary<string, string> scoreNames = scores.ToDictionary(
            score => score.Key,
            score => score.Value.ToString().ToLowerInvariant());

        return new ListingExtraction(extractedJson, data.ToJsonString(), JsonSerializer.Serialize(scoreNames));
    }

    private static List<FieldEvidence> ReadEvidence(JsonNode? node)
    {
        if (node is not JsonObject quotes)
        {
            return [];
        }

        List<FieldEvidence> evidence = [];
        foreach (KeyValuePair<string, JsonNode?> entry in quotes)
        {
            if (entry.Value is JsonValue value && value.TryGetValue(out string? quote) && !string.IsNullOrWhiteSpace(quote))
            {
                evidence.Add(new FieldEvidence(entry.Key, quote));
            }
        }

        return evidence;
    }
}
