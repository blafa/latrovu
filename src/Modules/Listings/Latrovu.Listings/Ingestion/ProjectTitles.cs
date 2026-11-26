using System.Text.Json.Nodes;

namespace Latrovu.Listings.Ingestion;

public static class ProjectTitles
{
    public static string From(ListingExtraction? building, string fileName)
    {
        JsonObject data = building is null ? [] : JsonNode.Parse(building.DataJson) as JsonObject ?? [];
        string street = string.Join(' ', new[] { TextAt(data, "location.street"), TextAt(data, "location.houseNumber") }.OfType<string>());
        string? city = TextAt(data, "location.city");
        if (street.Length == 0)
        {
            return Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
        }

        return city is null ? street : $"{street}, {city}";
    }

    private static string? TextAt(JsonObject data, string path) =>
        JsonPaths.Get(data, path) is JsonValue value && value.TryGetValue(out string? text) && !string.IsNullOrWhiteSpace(text) ? text.Trim() : null;
}
