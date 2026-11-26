using Latrovu.Listings.Extraction.Units;

namespace Latrovu.Listings.Tests.Extraction;

public sealed class UnitTablePagesTests
{
    private const string TablePage = """
        FLÄCHENANGEBOT
        ZIMMER ETAGE LAGE HNF M2 ANF M2 NETTO AKONTO NK BRUTTO
        3.5-ZWG EG links 62.3 5.0 2'700.00 210.00 2'910.00 Hier bewerben
        2.5-ZWG 1. OG mitte 38.5 - 1'890.00 130.00 2'020.00 Vermietet
        2.5-ZWG 5. OG ganze Etage 61.3 - 2'680.00 210.00 2'890.00 Hier bewerben
        """;

    private const string DescriptionPage = "LAGE\nDie Zypressenstrasse 3 befindet sich im beliebten Kreis 3.\nZürich HB 32 min 11 min 18 min";

    [Fact]
    public void Select_DocumentWithUnitTable_KeepsOnlyTablePages()
    {
        IReadOnlyList<string> pages = UnitTablePages.Select([DescriptionPage, TablePage, DescriptionPage]);

        Assert.Equal([TablePage], pages);
    }

    [Fact]
    public void Select_NoTableFound_KeepsAllPages()
    {
        string[] pages = [DescriptionPage, "Bezugstermin 1. April 2025"];

        Assert.Equal(pages, UnitTablePages.Select(pages));
    }

    [Fact]
    public void Complete_TwoOfThreeAmounts_ComputesTheThird()
    {
        ListingUnit noNet = new("1. OG mitte", 2.5m, 1, 38.5m, null, 130m, 2020m, null, null, UnitStatus.Rented);
        ListingUnit noGross = noNet with { NetRentChf = 1890m, GrossRentChf = null };
        ListingUnit onlyGross = noNet with { ChargesChf = null };

        Assert.Equal(1890m, UnitTableParser.Complete(noNet).NetRentChf);
        Assert.Equal(2020m, UnitTableParser.Complete(noGross).GrossRentChf);
        Assert.Null(UnitTableParser.Complete(onlyGross).NetRentChf);
    }
}
