using Latrovu.Listings;

namespace Latrovu.Listings.Tests;

internal sealed class FakeReviewSessionStore : IReviewSessionStore
{
    private readonly Dictionary<string, (Guid ListingId, DateTimeOffset ExpiresAt)> sessions = [];

    public Task CreateAsync(Guid listingId, string sessionHash, DateTimeOffset expiresAt, CancellationToken ct)
    {
        sessions[sessionHash] = (listingId, expiresAt);
        return Task.CompletedTask;
    }

    public Task<Guid?> FindListingIdAsync(string sessionHash, DateTimeOffset now, CancellationToken ct) =>
        Task.FromResult<Guid?>(
            sessions.TryGetValue(sessionHash, out (Guid ListingId, DateTimeOffset ExpiresAt) session) && session.ExpiresAt > now
                ? session.ListingId
                : null);
}
