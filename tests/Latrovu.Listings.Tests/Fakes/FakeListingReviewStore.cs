namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeListingReviewStore : IListingReviewStore
{
    public const string SourceEmail = "agent@example.com";
    private const string DraftStatus = "draft";
    private const string EmptyJson = "{}";

    public string DataJson { get; private set; } = EmptyJson;

    public string ScoresJson { get; private set; } = EmptyJson;

    public bool PublishContact { get; private set; }

    public string Status { get; private set; } = DraftStatus;

    public Task<ListingReview?> GetAsync(Guid listingId, CancellationToken ct) =>
        Task.FromResult<ListingReview?>(
            new ListingReview(listingId, Status, SourceEmail, null, DataJson, ScoresJson, EmptyJson, PublishContact, []));

    public Task SaveAsync(Guid listingId, string dataJson, string fieldScoresJson, CancellationToken ct)
    {
        DataJson = dataJson;
        ScoresJson = fieldScoresJson;
        return Task.CompletedTask;
    }

    public Task SetPublishContactAsync(Guid listingId, bool publishContact, CancellationToken ct)
    {
        PublishContact = publishContact;
        return Task.CompletedTask;
    }

    public Task<bool> ApproveAsync(Guid listingId, CancellationToken ct)
    {
        Status = ListingReview.ApprovedStatus;
        return Task.FromResult(true);
    }

    public Task<bool> WithdrawAsync(Guid listingId, CancellationToken ct)
    {
        Status = ListingReview.WithdrawnStatus;
        return Task.FromResult(true);
    }
}
