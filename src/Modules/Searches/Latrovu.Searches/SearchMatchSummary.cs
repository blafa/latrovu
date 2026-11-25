namespace Latrovu.Searches;

public sealed record SearchMatchSummary(int Sent, int AlreadySent)
{
    public MatchNews News => Sent > 0
        ? MatchNews.Sent
        : AlreadySent > 0 ? MatchNews.AlreadySent : MatchNews.NoneYet;
}
