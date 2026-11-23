using System.Text.Json.Nodes;
using Xunit;

namespace Latrovu.Listings.Tests.Review;

public sealed class FieldEditingTests
{
    [Fact]
    public void Catalog_ContainsTypedFieldsFromSchema()
    {
        Assert.Equal(FieldKind.Number, ListingFieldCatalog.Find("size.rooms")?.Kind);
        Assert.Equal(FieldKind.Boolean, ListingFieldCatalog.Find("features.balcony")?.Kind);
        Assert.Equal(FieldKind.TextList, ListingFieldCatalog.Find("features.flooring")?.Kind);
        Assert.Equal(FieldKind.ObjectList, ListingFieldCatalog.Find("parking")?.Kind);
        Assert.Equal(FieldKind.ObjectList, ListingFieldCatalog.Find("otherDetails")?.Kind);
        Assert.Contains("condominium", ListingFieldCatalog.Find("sale.ownershipType")!.Choices);
        Assert.Null(ListingFieldCatalog.Find("evidence"));
    }

    [Fact]
    public void Catalog_ObjectListHasTypedItemFields()
    {
        FieldDefinition parking = ListingFieldCatalog.Find("parking")!;

        Assert.Contains(parking.ItemFields, item => item.Key == "type" && item.Kind == FieldKind.Choice && item.Choices.Contains("carport"));
        Assert.Contains(parking.ItemFields, item => item.Key == "priceChf" && item.Kind == FieldKind.Number);
        Assert.Contains(parking.ItemFields, item => item.Key == "included" && item.Kind == FieldKind.Boolean);
    }

    [Fact]
    public void Parse_ObjectList_AcceptsListOfEntries()
    {
        JsonNode? value = FieldValueParser.Parse(ListingFieldCatalog.Find("parking")!, """[{"type":"carport","count":1}]""");

        Assert.Equal("carport", value![0]!["type"]!.GetValue<string>());
    }

    [Fact]
    public void Parse_ObjectList_RejectsNonList()
    {
        Assert.Throws<FormatException>(() => FieldValueParser.Parse(ListingFieldCatalog.Find("parking")!, """{"type":"carport"}"""));
    }

    [Theory]
    [InlineData("2'450", 2450)]
    [InlineData("2’450.50", 2450.5)]
    [InlineData("3,5", 3.5)]
    public void Parse_SwissNumbers(string raw, decimal expected)
    {
        JsonNode? value = FieldValueParser.Parse(ListingFieldCatalog.Find("rent.netChf")!, raw);

        Assert.Equal(expected, value!.GetValue<decimal>());
    }

    [Fact]
    public void Parse_InvalidChoice_Throws()
    {
        FieldDefinition field = ListingFieldCatalog.Find("sale.ownershipType")!;

        Assert.Throws<FormatException>(() => FieldValueParser.Parse(field, "castle"));
    }

    [Fact]
    public void Parse_TextList_SplitsAndTrims()
    {
        JsonNode? value = FieldValueParser.Parse(ListingFieldCatalog.Find("features.flooring")!, "Parkett, Platten ,");

        Assert.Equal(["Parkett", "Platten"], value!.AsArray().Select(item => item!.GetValue<string>()));
    }

    [Fact]
    public void Parse_Empty_ReturnsNull()
    {
        Assert.Null(FieldValueParser.Parse(ListingFieldCatalog.Find("title")!, "  "));
    }

    [Fact]
    public void JsonPaths_SetCreatesMissingParents()
    {
        JsonObject root = [];

        JsonPaths.Set(root, "size.rooms", JsonValue.Create(3.5m));

        Assert.Equal(3.5m, JsonPaths.Get(root, "size.rooms")!.GetValue<decimal>());
    }

    [Fact]
    public void AccessTokens_AreUrlSafeAndHashedConsistently()
    {
        string token = AccessTokens.Create();

        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
        Assert.Equal(AccessTokens.Hash(token), AccessTokens.Hash(token));
        Assert.NotEqual(AccessTokens.Hash(token), AccessTokens.Hash(AccessTokens.Create()));
    }
}
