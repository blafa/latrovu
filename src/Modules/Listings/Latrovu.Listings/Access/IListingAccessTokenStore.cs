namespace Latrovu.Listings.Access;

public interface IListingAccessTokenStore
{
    Task<string> CreateAsync(Guid listingId, CancellationToken ct);

    Task<Guid?> FindListingIdAsync(string token, CancellationToken ct);
}
