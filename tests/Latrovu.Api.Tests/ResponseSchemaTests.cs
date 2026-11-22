using System.Text.Json.Nodes;
using Latrovu.Api.Ai;
using Latrovu.Api.Listings;
using Xunit;

namespace Latrovu.Api.Tests;

public sealed class ResponseSchemaTests
{
    public static TheoryData<JsonResponseSchema> Schemas => new()
    {
        ListingSchema.Value,
        DocumentClassificationSchema.Value
    };

    [Theory]
    [MemberData(nameof(Schemas))]
    public void Schema_IsValidJsonObjectWithProperties(JsonResponseSchema schema)
    {
        JsonNode? root = JsonNode.Parse(schema.Schema);

        Assert.NotNull(root);
        Assert.Equal("object", root["type"]?.GetValue<string>());
        Assert.NotNull(root["properties"]);
        Assert.False(string.IsNullOrWhiteSpace(schema.Name));
    }

    [Fact]
    public void ListingSchema_RequiredFieldsExistAsProperties()
    {
        JsonNode root = JsonNode.Parse(ListingSchema.Value.Schema)!;
        JsonObject properties = root["properties"]!.AsObject();

        foreach (JsonNode? required in root["required"]!.AsArray())
        {
            Assert.True(properties.ContainsKey(required!.GetValue<string>()));
        }
    }
}
