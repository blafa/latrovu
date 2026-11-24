namespace Latrovu.Listings.Viewing;

public interface IListingViewLinkStore
{
    Task<string> CreateAsync(Guid listingId, Guid? searchId, DateTimeOffset expiresAt, CancellationToken ct);

    Task<Guid?> FindListingIdAsync(string token, DateTimeOffset now, CancellationToken ct);

    Task RegisterViewAsync(string token, DateTimeOffset now, CancellationToken ct);
}
