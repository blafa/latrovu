namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeListingAccessTokenStore(string validToken, Guid listingId) : IListingAccessTokenStore
{
    private readonly Dictionary<string, Guid> tokens = new() { [validToken] = listingId };

    public void Add(string token, Guid listingIdForToken) => tokens[token] = listingIdForToken;

    public Task<string> CreateAsync(Guid listingIdToLink, CancellationToken ct) => Task.FromResult(validToken);

    public Task<Guid?> FindListingIdAsync(string token, CancellationToken ct) =>
        Task.FromResult<Guid?>(tokens.TryGetValue(token, out Guid found) ? found : null);
}
