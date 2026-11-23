namespace Latrovu.Listings.Tests.Extraction;

public sealed class EvidenceLocatorTests
{
    [Fact]
    public void Locate_FindsAcrossApostropheDashAndLineBreak()
    {
        string[] pages = ["Deckblatt", "Der Verkaufspreis beträgt\nCHF 422’000.– inkl. Keller"];

        EvidenceLocation? location = EvidenceLocator.Locate(pages, "beträgt CHF 422'000.-");

        Assert.NotNull(location);
        Assert.Equal(2, location.Page);
        Assert.Equal("beträgt CHF 422’000.–", location.Match);
        Assert.Equal("Der Verkaufspreis ", location.Before);
        Assert.Equal(" inkl. Keller", location.After);
    }

    [Fact]
    public void Locate_IsCaseInsensitive()
    {
        EvidenceLocation? location = EvidenceLocator.Locate(["2.5 ZIMMER WOHNUNG IM OG OST"], "2.5 Zimmer Wohnung");

        Assert.Equal("2.5 ZIMMER WOHNUNG", location?.Match);
    }

    [Fact]
    public void Locate_LongContext_IsShortenedAtWordBoundaries()
    {
        string filler = string.Join(' ', Enumerable.Repeat("Wort", 60));
        EvidenceLocation? location = EvidenceLocator.Locate([$"{filler} Kandergrund {filler}"], "Kandergrund");

        Assert.StartsWith("…", location!.Before);
        Assert.EndsWith("…", location.After);
        Assert.True(location.Before.Length <= EvidenceLocator.ContextCharacters + 1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Zürich")]
    public void Locate_MissingOrEmptyNeedle_ReturnsNull(string needle)
    {
        Assert.Null(EvidenceLocator.Locate(["Kandergrund"], needle));
    }
}
