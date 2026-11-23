namespace Latrovu.Listings;

public interface IListingAccessTokenStore
{
    Task<string> CreateAsync(Guid listingId, CancellationToken ct);

    Task<Guid?> FindListingIdAsync(string token, CancellationToken ct);
}
