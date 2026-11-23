namespace Latrovu.Listings.Review;

public interface IListingReviewStore
{
    Task<ListingReview?> GetAsync(Guid listingId, CancellationToken ct);

    Task SaveAsync(Guid listingId, string dataJson, string fieldScoresJson, CancellationToken ct);

    Task SetPublishContactAsync(Guid listingId, bool publishContact, CancellationToken ct);

    Task<bool> ApproveAsync(Guid listingId, CancellationToken ct);
}
