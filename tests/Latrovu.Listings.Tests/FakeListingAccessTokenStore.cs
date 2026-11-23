using Latrovu.Listings;

namespace Latrovu.Listings.Tests;

internal sealed class FakeListingAccessTokenStore(string validToken, Guid listingId) : IListingAccessTokenStore
{
    public Task<string> CreateAsync(Guid listingIdToLink, CancellationToken ct) => Task.FromResult(validToken);

    public Task<Guid?> FindListingIdAsync(string token, CancellationToken ct) =>
        Task.FromResult<Guid?>(token == validToken ? listingId : null);
}
