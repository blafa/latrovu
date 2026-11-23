using System.Text.Json.Nodes;
using Xunit;

namespace Latrovu.Listings.Tests.Extraction;

public sealed class EvidenceScorerTests
{
    private const string Expose =
        "Schöne 2.5-Zimmer-Wohnung in Kandergrund, 1. Obergeschoss. Verkaufspreis CHF 422’000.–, Baujahr 2022, 117.50 m²";

    [Fact]
    public void Score_QuoteFoundInText_IsHigh()
    {
        JsonObject data = Data("""{ "size": { "rooms": 2.5 } }""");

        IReadOnlyDictionary<string, FieldConfidence> scores =
            EvidenceScorer.Score(Expose, data, [new FieldEvidence("size.rooms", "2.5-Zimmer-Wohnung")]);

        Assert.Equal(FieldConfidence.High, scores["size.rooms"]);
    }

    [Fact]
    public void Score_QuoteWithDifferentApostropheAndSpacing_IsHigh()
    {
        JsonObject data = Data("""{ "sale": { "priceChf": 1 } }""");

        IReadOnlyDictionary<string, FieldConfidence> scores =
            EvidenceScorer.Score(Expose, data, [new FieldEvidence("sale.priceChf", "Verkaufspreis  CHF 422'000.–")]);

        Assert.Equal(FieldConfidence.High, scores["sale.priceChf"]);
    }

    [Fact]
    public void Score_QuoteNotInTextAndValueNotFound_IsMedium()
    {
        JsonObject data = Data("""{ "size": { "rooms": 3.5 } }""");

        IReadOnlyDictionary<string, FieldConfidence> scores =
            EvidenceScorer.Score(Expose, data, [new FieldEvidence("size.rooms", "3.5 Zimmer")]);

        Assert.Equal(FieldConfidence.Medium, scores["size.rooms"]);
    }

    [Theory]
    [InlineData("""{ "sale": { "priceChf": 422000 } }""", "sale.priceChf")]
    [InlineData("""{ "building": { "yearBuilt": 2022 } }""", "building.yearBuilt")]
    [InlineData("""{ "size": { "livingAreaM2": 117.5 } }""", "size.livingAreaM2")]
    [InlineData("""{ "location": { "city": "Kandergrund" } }""", "location.city")]
    public void Score_ValueFoundInTextWithoutQuote_IsHigh(string json, string path)
    {
        IReadOnlyDictionary<string, FieldConfidence> scores = EvidenceScorer.Score(Expose, Data(json), []);

        Assert.Equal(FieldConfidence.High, scores[path]);
    }

    [Theory]
    [InlineData("""{ "building": { "floor": 1 } }""", "building.floor")]
    [InlineData("""{ "offerType": "sale" }""", "offerType")]
    [InlineData("""{ "location": { "city": "Bern" } }""", "location.city")]
    public void Score_ValueNotVerifiableWithoutQuote_IsLow(string json, string path)
    {
        IReadOnlyDictionary<string, FieldConfidence> scores = EvidenceScorer.Score(Expose, Data(json), []);

        Assert.Equal(FieldConfidence.Low, scores[path]);
    }

    [Fact]
    public void Score_EmptyOrMissingValues_AreNotScored()
    {
        JsonObject data = Data("""{ "location": { "city": "", "zip": null } }""");

        IReadOnlyDictionary<string, FieldConfidence> scores = EvidenceScorer.Score(Expose, data, []);

        Assert.Empty(scores);
    }

    private static JsonObject Data(string json) => JsonNode.Parse(json)!.AsObject();
}
