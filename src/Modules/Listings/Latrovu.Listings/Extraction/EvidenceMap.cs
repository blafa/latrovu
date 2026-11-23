using System.Text.Json.Nodes;

namespace Latrovu.Listings.Extraction;

public static class EvidenceMap
{
    private const char PathSeparator = '.';

    public static IReadOnlyDictionary<string, EvidenceLocation> Build(
        IReadOnlyList<string> pages,
        JsonObject data,
        IEnumerable<FieldEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(evidence);

        Dictionary<string, string> quotesByField = evidence
            .GroupBy(item => item.Field)
            .ToDictionary(group => group.Key, group => group.First().Quote);

        Dictionary<string, EvidenceLocation> locations = [];
        foreach ((string path, JsonValue value) in Leaves(data, string.Empty))
        {
            EvidenceLocation? location = Needles(path, value, quotesByField)
                .Select(needle => EvidenceLocator.Locate(pages, needle))
                .FirstOrDefault(found => found is not null);
            if (location is not null)
            {
                locations[path] = location;
            }
        }

        return locations;
    }

    private static IEnumerable<string> Needles(string path, JsonValue value, Dictionary<string, string> quotesByField)
    {
        if (quotesByField.TryGetValue(path, out string? quote))
        {
            yield return quote;
        }

        if (ListingKeyFields.InferredPaths.Contains(path))
        {
            yield break;
        }

        foreach (string needle in EvidenceNeedles.FromValue(value))
        {
            yield return needle;
        }
    }

    private static IEnumerable<(string Path, JsonValue Value)> Leaves(JsonObject node, string prefix)
    {
        foreach (KeyValuePair<string, JsonNode?> property in node)
        {
            string path = prefix.Length == 0 ? property.Key : $"{prefix}{PathSeparator}{property.Key}";
            if (property.Value is JsonObject child)
            {
                foreach ((string Path, JsonValue Value) leaf in Leaves(child, path))
                {
                    yield return leaf;
                }
            }
            else if (property.Value is JsonValue value)
            {
                yield return (path, value);
            }
        }
    }
}
