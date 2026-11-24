namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeListingViewLinkStore : IListingViewLinkStore
{
    private readonly Dictionary<string, (Guid ListingId, DateTimeOffset ExpiresAt)> links = [];
    private int nextToken;

    public Dictionary<string, int> Views { get; } = [];

    public Task<string> CreateAsync(Guid listingId, Guid? searchId, DateTimeOffset expiresAt, CancellationToken ct)
    {
        string token = $"view-{++nextToken}";
        links[token] = (listingId, expiresAt);
        return Task.FromResult(token);
    }

    public Task<Guid?> FindListingIdAsync(string token, DateTimeOffset now, CancellationToken ct) =>
        Task.FromResult<Guid?>(
            links.TryGetValue(token, out (Guid ListingId, DateTimeOffset ExpiresAt) link) && link.ExpiresAt > now
                ? link.ListingId
                : null);

    public Task RegisterViewAsync(string token, DateTimeOffset now, CancellationToken ct)
    {
        Views[token] = Views.GetValueOrDefault(token) + 1;
        return Task.CompletedTask;
    }
}
