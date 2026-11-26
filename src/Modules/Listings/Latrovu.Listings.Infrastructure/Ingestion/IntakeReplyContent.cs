using Latrovu.Listings.Infrastructure.Review.Web;

namespace Latrovu.Listings.Infrastructure.Ingestion;

internal sealed record IntakeReplyContent(
    IntakeReport Report,
    IReadOnlyDictionary<Guid, IntakeListingSummary> Summaries,
    Func<string, string> ReviewUrl)
{
    public IntakeListingSummary? SummaryOf(IntakeFileOutcome file) =>
        file.ListingId is { } listingId ? Summaries.GetValueOrDefault(listingId) : null;
}
