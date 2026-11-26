using System.Text.Json;
using System.Text.Json.Nodes;
using Latrovu.Listings.Extraction.Units;

namespace Latrovu.Listings.Tests.Extraction;

public sealed class UnitListingComposerTests
{
    private const string BuildingData = """
        {"title":"Willkommen in Ihrem neuen Zuhause","referenceNumber":"10","location":{"street":"Zypressenstrasse","houseNumber":"3","city":"Zürich"},
         "size":{"rooms":3.5,"livingAreaM2":61.3,"usableAreaM2":8,"balconyAreaM2":5},"building":{"floor":4},
         "rent":{"netChf":2410,"chargesChf":160,"grossChf":2570},"availability":{"availableFrom":"2025-04-01"},"features":{"cellar":true}}
        """;

    private const string BuildingScores = """{"size.livingAreaM2":"medium","rent.grossChf":"low","features.cellar":"high"}""";

    private const string BuildingEvidence = """{"size.livingAreaM2":{"page":3,"before":"","match":"61.3","after":""},"features.cellar":{"page":7,"before":"","match":"Keller","after":""}}""";

    private static readonly ListingExtraction Building = new("{}", BuildingData, BuildingScores, BuildingEvidence);

    [Fact]
    public void Compose_OverwritesUnitFieldsAndKeepsBuildingFields()
    {
        ListingUnit unit = new("1. OG links", 2.5m, 1, 46.9m, 2210m, 160m, 2370m, null, null, UnitStatus.Available);

        ListingExtraction listing = UnitListingComposer.Compose(Building, unit);

        JsonObject data = (JsonObject)JsonNode.Parse(listing.DataJson)!;
        Assert.Equal("2½-Zimmer-Wohnung, 1. OG links", (string?)data["title"]);
        Assert.Equal(46.9m, (decimal?)data["size"]!["livingAreaM2"]);
        Assert.Equal(1, (int?)data["building"]!["floor"]);
        Assert.Equal(2370m, (decimal?)data["rent"]!["grossChf"]);
        Assert.Equal("2025-04-01", (string?)data["availability"]!["availableFrom"]);
        Assert.Null(data["referenceNumber"]);
        Assert.Null(data["size"]!["usableAreaM2"]);
        Assert.True((bool?)data["features"]!["cellar"]);

        Dictionary<string, string> scores = JsonSerializer.Deserialize<Dictionary<string, string>>(listing.FieldScoresJson)!;
        Assert.Equal("high", scores["size.livingAreaM2"]);
        Assert.Equal("high", scores["rent.grossChf"]);
        Assert.DoesNotContain("size.livingAreaM2", listing.FieldEvidenceJson, StringComparison.Ordinal);
        Assert.Contains("features.cellar", listing.FieldEvidenceJson, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_MissingUnitValue_ClearsTheBuildingValue()
    {
        ListingUnit unit = new("4. OG rechts", 3.5m, 4, null, 2630m, 210m, 2840m, null, null, UnitStatus.Available);

        ListingExtraction listing = UnitListingComposer.Compose(Building, unit);

        Assert.Null(JsonNode.Parse(listing.DataJson)!["size"]!["livingAreaM2"]);
        Assert.DoesNotContain("size.livingAreaM2", listing.FieldScoresJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectTitle_UsesStreetAndCity()
    {
        Assert.Equal("Zypressenstrasse 3, Zürich", ProjectTitles.From(Building, "doc.pdf"));
        Assert.Equal("doc", ProjectTitles.From(null, "doc.pdf"));
    }
}
