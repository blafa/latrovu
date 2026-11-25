namespace Latrovu.Searches.Tests;

public sealed class SearchMatchSummaryTests
{
    [Theory]
    [InlineData(2, 3, MatchNews.Sent)]
    [InlineData(0, 3, MatchNews.AlreadySent)]
    [InlineData(0, 0, MatchNews.NoneYet)]
    public void News_ReflectsWhatWasSent(int sent, int alreadySent, MatchNews expected)
    {
        SearchMatchSummary summary = new(sent, alreadySent);

        Assert.Equal(expected, summary.News);
    }
}
