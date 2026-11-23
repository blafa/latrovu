using System.Text.Json.Nodes;

namespace Latrovu.BuildingBlocks.Ai;

public static class JsonSchemaSealer
{
    private const string PropertiesKey = "properties";
    private const string AdditionalPropertiesKey = "additionalProperties";

    public static string Seal(string schemaJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaJson);

        JsonNode root = JsonNode.Parse(schemaJson) ?? throw new ArgumentException("Schema is empty", nameof(schemaJson));
        SealNode(root);
        return root.ToJsonString();
    }

    private static void SealNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject schema:
                if (schema[PropertiesKey] is JsonObject && schema[AdditionalPropertiesKey] is null)
                {
                    schema[AdditionalPropertiesKey] = false;
                }

                foreach (KeyValuePair<string, JsonNode?> child in schema.ToList())
                {
                    SealNode(child.Value);
                }

                break;
            case JsonArray items:
                foreach (JsonNode? item in items)
                {
                    SealNode(item);
                }

                break;
        }
    }
}
