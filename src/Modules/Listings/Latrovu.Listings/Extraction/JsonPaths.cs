using System.Text.Json.Nodes;

namespace Latrovu.Listings.Extraction;

public static class JsonPaths
{
    private const char Separator = '.';

    public static JsonNode? Get(JsonObject root, string path)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        JsonNode? node = root;
        foreach (string segment in path.Split(Separator))
        {
            node = node is JsonObject parent ? parent[segment] : null;
            if (node is null)
            {
                return null;
            }
        }

        return node;
    }

    public static void Set(JsonObject root, string path, JsonNode? value)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string[] segments = path.Split(Separator);
        JsonObject parent = root;
        foreach (string segment in segments[..^1])
        {
            if (parent[segment] is not JsonObject child)
            {
                child = [];
                parent[segment] = child;
            }

            parent = child;
        }

        parent[segments[^1]] = value;
    }
}
