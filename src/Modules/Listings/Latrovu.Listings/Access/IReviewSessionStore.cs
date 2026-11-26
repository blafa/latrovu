namespace Latrovu.Listings.Access;

public interface IReviewSessionStore
{
    Task CreateAsync(string sourceEmail, string sessionHash, DateTimeOffset expiresAt, CancellationToken ct);

    Task<string?> FindSourceEmailAsync(string sessionHash, DateTimeOffset now, CancellationToken ct);

    Task DeleteAsync(string sessionHash, CancellationToken ct);
}
