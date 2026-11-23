using System.Text.Json.Nodes;

namespace Latrovu.Listings.Tests.Extraction;

public sealed class ListingExtractionBuilderTests
{
    private const string Expose = "Schöne 2.5-Zimmer-Wohnung in Kandergrund. Verkaufspreis CHF 422’000.– inklusive Keller.";

    [Fact]
    public void Build_RemovesEvidenceFromDataAndKeepsItInOriginal()
    {
        const string extracted = """
            { "size": { "rooms": 2.5 }, "evidence": { "size.rooms": "2.5-Zimmer" } }
            """;

        ListingExtraction extraction = ListingExtractionBuilder.Build(extracted, [Expose]);

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

        ListingExtraction extraction = ListingExtractionBuilder.Build(extracted, [Expose]);
        JsonNode scores = JsonNode.Parse(extraction.FieldScoresJson)!;

        Assert.Equal("high", scores["size.rooms"]!.GetValue<string>());
        Assert.Equal("low", scores["location.city"]!.GetValue<string>());
    }

    [Fact]
    public void Build_StoresEvidenceSnippetsWithPageAndContext()
    {
        const string extracted = """{ "sale": { "priceChf": 422000 }, "location": { "city": "Kandergrund" }, "evidence": {} }""";

        ListingExtraction extraction = ListingExtractionBuilder.Build(extracted, ["Titelseite", Expose]);
        JsonNode evidence = JsonNode.Parse(extraction.FieldEvidenceJson)!;

        Assert.Equal(2, evidence["sale.priceChf"]!["page"]!.GetValue<int>());
        Assert.Equal("422’000", evidence["sale.priceChf"]!["match"]!.GetValue<string>());
        Assert.Contains("Verkaufspreis CHF", evidence["sale.priceChf"]!["before"]!.GetValue<string>());
        Assert.Equal("Kandergrund", evidence["location.city"]!["match"]!.GetValue<string>());
    }

    [Fact]
    public void Build_NonObjectAnswer_KeepsAnswerWithoutScores()
    {
        ListingExtraction extraction = ListingExtractionBuilder.Build("[1, 2]", [Expose]);

        Assert.Equal("[1, 2]", extraction.DataJson);
        Assert.Equal("{}", extraction.FieldScoresJson);
        Assert.Equal("{}", extraction.FieldEvidenceJson);
    }
}
