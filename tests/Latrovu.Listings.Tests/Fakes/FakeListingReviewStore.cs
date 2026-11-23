
namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeListingReviewStore : IListingReviewStore
{
    private const string DraftStatus = "draft";
    private const string EmptyJson = "{}";
    public const string SourceEmail = "agent@example.com";

    public string DataJson { get; private set; } = EmptyJson;

    public string ScoresJson { get; private set; } = EmptyJson;

    public Task<ListingReview?> GetAsync(Guid listingId, CancellationToken ct) =>
        Task.FromResult<ListingReview?>(new ListingReview(listingId, DraftStatus, SourceEmail, null, DataJson, ScoresJson, []));

    public Task SaveAsync(Guid listingId, string dataJson, string fieldScoresJson, CancellationToken ct)
    {
        DataJson = dataJson;
        ScoresJson = fieldScoresJson;
        return Task.CompletedTask;
    }

    public Task<bool> ApproveAsync(Guid listingId, CancellationToken ct) => Task.FromResult(true);
}
