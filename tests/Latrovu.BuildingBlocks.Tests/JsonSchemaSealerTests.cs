using System.Text.Json.Nodes;
using Latrovu.BuildingBlocks.Ai;
using Xunit;

namespace Latrovu.BuildingBlocks.Tests;

public sealed class JsonSchemaSealerTests
{
    [Fact]
    public void Seal_AddsAdditionalPropertiesFalseToNestedObjects()
    {
        const string schema = """
            { "type": "object", "properties": { "sale": { "type": "object", "properties": { "priceChf": { "type": "number" } } } } }
            """;

        JsonNode sealedSchema = JsonNode.Parse(JsonSchemaSealer.Seal(schema))!;

        Assert.False(sealedSchema["additionalProperties"]!.GetValue<bool>());
        Assert.False(sealedSchema["properties"]!["sale"]!["additionalProperties"]!.GetValue<bool>());
    }

    [Fact]
    public void Seal_KeepsExplicitAdditionalProperties()
    {
        const string schema = """{ "type": "object", "properties": {}, "additionalProperties": true }""";

        JsonNode sealedSchema = JsonNode.Parse(JsonSchemaSealer.Seal(schema))!;

        Assert.True(sealedSchema["additionalProperties"]!.GetValue<bool>());
    }

    [Fact]
    public void Seal_SealsObjectsInsideArrayItems()
    {
        const string schema = """
            { "type": "object", "properties": { "parking": { "type": "array", "items": { "type": "object", "properties": {} } } } }
            """;

        JsonNode sealedSchema = JsonNode.Parse(JsonSchemaSealer.Seal(schema))!;

        Assert.False(sealedSchema["properties"]!["parking"]!["items"]!["additionalProperties"]!.GetValue<bool>());
    }
}
