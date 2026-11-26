using Latrovu.Listings.Extraction.Units;

namespace Latrovu.Listings.Tests.Extraction;

public sealed class UnitTableParserTests
{
    [Fact]
    public void Parse_UnitTable_ReturnsOneUnitPerApartment()
    {
        const string json = """
            {"units":[
              {"label":"EG links","rooms":3.5,"floor":0,"livingAreaM2":62.3,"netRentChf":2700,"chargesChf":210,"grossRentChf":2910,"priceChf":null,"availableFrom":"2025-04-01","status":"available"},
              {"label":"1. OG rechts","rooms":2.5,"floor":1.0,"livingAreaM2":48,"netRentChf":2150,"chargesChf":180,"grossRentChf":2330,"priceChf":null,"availableFrom":null,"status":"rented"}
            ]}
            """;

        IReadOnlyList<ListingUnit> units = UnitTableParser.Parse(json);

        Assert.Equal(2, units.Count);
        Assert.Equal(new ListingUnit("EG links", 3.5m, 0, 62.3m, 2700m, 210m, 2910m, null, "2025-04-01", UnitStatus.Available), units[0]);
        Assert.Equal(1, units[1].Floor);
        Assert.Equal(UnitStatus.Rented, units[1].Status);
    }

    [Fact]
    public void Parse_DuplicatesAndEmptyEntries_AreDropped()
    {
        const string json = """
            {"units":[
              {"label":"Whg 2.1","rooms":3.5,"floor":2,"livingAreaM2":80,"netRentChf":null,"chargesChf":null,"grossRentChf":null,"priceChf":null,"availableFrom":null,"status":null},
              {"label":"whg 2.1","rooms":3.5,"floor":2,"livingAreaM2":80,"netRentChf":null,"chargesChf":null,"grossRentChf":null,"priceChf":null,"availableFrom":null,"status":null},
              {"label":"Keller","rooms":null,"floor":-1,"livingAreaM2":null,"netRentChf":null,"chargesChf":null,"grossRentChf":null,"priceChf":null,"availableFrom":null,"status":null},
              {"label":"","rooms":2.5,"floor":1,"livingAreaM2":50,"netRentChf":null,"chargesChf":null,"grossRentChf":null,"priceChf":null,"availableFrom":null,"status":null}
            ]}
            """;

        IReadOnlyList<ListingUnit> units = UnitTableParser.Parse(json);

        ListingUnit unit = Assert.Single(units);
        Assert.Equal("Whg 2.1", unit.Label);
        Assert.Equal(UnitStatus.Unknown, unit.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"something\":1}")]
    public void Parse_UnusableAnswer_ReturnsNoUnits(string? json)
    {
        Assert.Empty(UnitTableParser.Parse(json));
    }
}
