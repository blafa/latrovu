namespace Latrovu.Listings.Viewing;

public interface IListingViewService
{
    Task<string?> CreateLinkAsync(Guid listingId, Guid? searchId, CancellationToken ct);

    Task<ListingReview?> OpenAsync(string token, CancellationToken ct);

    Task<ListingReview?> FindAsync(string token, CancellationToken ct);
}
