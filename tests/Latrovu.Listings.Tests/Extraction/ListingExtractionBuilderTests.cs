using System.Text.Json.Nodes;
using Xunit;

namespace Latrovu.Listings.Tests.Extraction;

public sealed class ListingExtractionBuilderTests
{
    private const string Expose = "2.5-Zimmer-Wohnung in Kandergrund";

    [Fact]
    public void Build_RemovesEvidenceFromDataAndKeepsItInOriginal()
    {
        const string extracted = """
            { "size": { "rooms": 2.5 }, "evidence": { "size.rooms": "2.5-Zimmer" } }
            """;

        ListingExtraction extraction = ListingExtractionBuilder.Build(extracted, Expose);

        Assert.Null(JsonNode.Parse(extraction.DataJson)!["evidence"]);
        Assert.NotNull(JsonNode.Parse(extraction.ExtractedJson)!["evidence"]);
    }

    [Fact]
    public void Build_WritesScoresAsLowercaseNames()
    {
        const string extracted = """
            { "size": { "rooms": 2.5 }, "location": { "city": "Bern" },
              "evidence": { "size.rooms": "2.5-Zimmer", "location.city": null } }
            """;

        ListingExtraction extraction = ListingExtractionBuilder.Build(extracted, Expose);
        JsonNode scores = JsonNode.Parse(extraction.FieldScoresJson)!;

        Assert.Equal("high", scores["size.rooms"]!.GetValue<string>());
        Assert.Equal("low", scores["location.city"]!.GetValue<string>());
    }

    [Fact]
    public void Build_NonObjectAnswer_KeepsAnswerWithoutScores()
    {
        ListingExtraction extraction = ListingExtractionBuilder.Build("[1, 2]", Expose);

        Assert.Equal("[1, 2]", extraction.DataJson);
        Assert.Equal("{}", extraction.FieldScoresJson);
    }
}
