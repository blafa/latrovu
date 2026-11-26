using System.Text.Json.Nodes;

namespace Latrovu.Searches;

public static class RefinementFilter
{
    private static readonly Dictionary<string, string[]> CriteriaByTopic = new(StringComparer.OrdinalIgnoreCase)
    {
        ["offerType"] = ["offerType"],
        ["place"] = ["places"],
        ["rooms"] = ["roomsMin", "roomsMax"],
        ["budget"] = ["priceMaxChf"],
        ["livingArea"] = ["livingAreaMinM2"],
        ["moveIn"] = ["availableFrom"]
    };

    public static IReadOnlyList<string> Keep(IEnumerable<Refinement> refinements, string criteriaJson)
    {
        ArgumentNullException.ThrowIfNull(refinements);

        JsonObject criteria = JsonNode.Parse(string.IsNullOrWhiteSpace(criteriaJson) ? "{}" : criteriaJson) as JsonObject ?? [];
        return refinements
            .Where(refinement => !string.IsNullOrWhiteSpace(refinement.Question) && !IsAnswered(refinement.Topic, criteria))
            .Select(refinement => refinement.Question.Trim())
            .ToList();
    }

    private static bool IsAnswered(string topic, JsonObject criteria) =>
        CriteriaByTopic.TryGetValue(topic, out string[]? fields) && fields.Any(field => HasValue(criteria[field]));

    private static bool HasValue(JsonNode? node) => node switch
    {
        null => false,
        JsonArray array => array.Count > 0,
        JsonValue value when value.TryGetValue(out string? text) => !string.IsNullOrWhiteSpace(text),
        _ => true
    };
}
