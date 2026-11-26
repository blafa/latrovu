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

    public List<string> ImageKeys { get; } = [];

    public Dictionary<Guid, string> SenderByListing { get; } = [];

    public Task<ListingReview?> GetAsync(Guid listingId, CancellationToken ct) =>
        Task.FromResult<ListingReview?>(
            new ListingReview(listingId, Status, SenderByListing.GetValueOrDefault(listingId, SourceEmail), null, DataJson, ScoresJson, EmptyJson, PublishContact, []));

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

    public Task<WithdrawnListing?> WithdrawAsync(Guid listingId, CancellationToken ct)
    {
        if (Status != ListingReview.ApprovedStatus)
        {
            return Task.FromResult<WithdrawnListing?>(null);
        }

        Status = ListingReview.WithdrawnStatus;
        return Task.FromResult<WithdrawnListing?>(new WithdrawnListing(ImageKeys));
    }
}
