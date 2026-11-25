namespace Latrovu.Listings.Viewing;

public interface IListingViewService
{
    Task<string?> CreateLinkAsync(Guid listingId, Guid? searchId, CancellationToken ct);

    Task<string?> ShareAsync(string token, CancellationToken ct);

    Task<ListingView?> OpenAsync(string token, CancellationToken ct);

    Task<ListingReview?> FindAsync(string token, CancellationToken ct);

    Task<ListingReview?> FindPersonalAsync(string token, CancellationToken ct);

    Task<ListingReview?> FindSharedAsync(string token, CancellationToken ct);
}
