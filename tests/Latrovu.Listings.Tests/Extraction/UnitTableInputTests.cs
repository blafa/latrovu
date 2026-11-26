using Latrovu.Listings.Extraction.Units;

namespace Latrovu.Listings.Tests.Extraction;

public sealed class UnitTableInputTests
{
    private const string MzpTablePage = """
        FLÄCHENANGEBOT
        ZIMMER ETAGE LAGE HNF M2 ANF M2 NETTO AKONTO NK BRUTTO BEWERBEN
        3.5-ZWG EG links 62.3 5.0 2'700.00 210.00 2'910.00 Hier bewerben
        3.5-ZWG EG rechts 62.9 5.0 2'700.00 210.00 2'910.00 Vermietet
        2.5-ZWG 1. OG links 46.9 8.6 2'210.00 160.00 2'370.00 Vermietet
        2.5-ZWG 1. OG mitte 38.5 - 1'890.00 130.00 2'020.00 Vermietet
        2.5-ZWG 5. OG ganze Etage 61.3 - 2'680.00 210.00 2'890.00 Hier bewerben
        3
        MZP AG, Mittelstrasse 14, 8008 Zürich
        """;

    [Fact]
    public void Find_TablePage_ReturnsHeaderAndPriceRows()
    {
        UnitTableInput table = UnitTableInput.Find(["Erstvermietung ab 1. April 2025", MzpTablePage]);

        Assert.Equal("ZIMMER ETAGE LAGE HNF M2 ANF M2 NETTO AKONTO NK BRUTTO BEWERBEN", table.Header);
        Assert.Equal(5, table.Rows.Count);
        Assert.True(table.HasRows);
        Assert.StartsWith("Kopfzeile: ZIMMER", table.ToPrompt());
        Assert.Contains("Zeile 4: 2.5-ZWG 1. OG mitte", table.ToPrompt());
    }

    [Fact]
    public void Parse_WithRows_DropsValuesFromAnotherRowAndReadsStatusFromRow()
    {
        UnitTableInput table = UnitTableInput.Find([MzpTablePage]);
        const string json = """
            {"units":[
              {"row":1,"label":"EG links","rooms":3.5,"floor":0,"livingAreaM2":62.3,"netRentChf":2700,"chargesChf":210,"grossRentChf":2910,"priceChf":null,"availableFrom":null,"status":"rented"},
              {"row":3,"label":"1. OG links","rooms":2.5,"floor":1,"livingAreaM2":46.9,"netRentChf":1890,"chargesChf":130,"grossRentChf":2020,"priceChf":null,"availableFrom":null,"status":"available"}
            ]}
            """;

        IReadOnlyList<ListingUnit> units = UnitTableParser.Parse(json, table.Rows);

        Assert.Equal(UnitStatus.Available, units[0].Status);
        Assert.Equal(2910m, units[0].GrossRentChf);
        Assert.Equal(UnitStatus.Rented, units[1].Status);
        Assert.Equal(46.9m, units[1].LivingAreaM2);
        Assert.Null(units[1].NetRentChf);
        Assert.Null(units[1].GrossRentChf);
    }
}
