namespace Latrovu.Listings.Viewing;

public interface IListingViewLinkStore
{
    Task<string> CreateAsync(Guid listingId, Guid? searchId, DateTimeOffset expiresAt, CancellationToken ct);

    Task<string> CreateSharedAsync(Guid listingId, DateTimeOffset expiresAt, CancellationToken ct);

    Task CreateFromHashAsync(Guid listingId, Guid searchId, string tokenHash, DateTimeOffset expiresAt, string? seekerAddress, CancellationToken ct);

    Task<ListingViewLink?> FindAsync(string token, DateTimeOffset now, CancellationToken ct);

    Task RegisterViewAsync(string token, DateTimeOffset now, CancellationToken ct);
}
