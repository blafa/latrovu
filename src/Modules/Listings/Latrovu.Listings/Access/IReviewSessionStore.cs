namespace Latrovu.Listings.Access;

public interface IReviewSessionStore
{
    Task CreateAsync(Guid listingId, string sessionHash, DateTimeOffset expiresAt, CancellationToken ct);

    Task<Guid?> FindListingIdAsync(string sessionHash, DateTimeOffset now, CancellationToken ct);
}
