namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeListingViewLinkStore : IListingViewLinkStore
{
    private readonly Dictionary<string, ListingViewLink> links = [];
    private int nextToken;

    public Dictionary<string, int> Views { get; } = [];

    public Task<string> CreateAsync(Guid listingId, Guid? searchId, DateTimeOffset expiresAt, CancellationToken ct) =>
        Task.FromResult(Add(new ListingViewLink(listingId, false, expiresAt)));

    public Task<string> CreateSharedAsync(Guid listingId, DateTimeOffset expiresAt, CancellationToken ct) =>
        Task.FromResult(Add(new ListingViewLink(listingId, true, expiresAt)));

    public Task CreateFromHashAsync(Guid listingId, Guid searchId, string tokenHash, DateTimeOffset expiresAt, string? seekerAddress, CancellationToken ct)
    {
        links[tokenHash] = new ListingViewLink(listingId, false, expiresAt);
        return Task.CompletedTask;
    }

    public Task<ListingViewLink?> FindAsync(string token, DateTimeOffset now, CancellationToken ct) =>
        Task.FromResult(links.TryGetValue(token, out ListingViewLink? link) && link.ExpiresAt > now ? link : null);

    public Task RegisterViewAsync(string token, DateTimeOffset now, CancellationToken ct)
    {
        Views[token] = Views.GetValueOrDefault(token) + 1;
        return Task.CompletedTask;
    }

    private string Add(ListingViewLink link)
    {
        string token = $"view-{++nextToken}";
        links[token] = link;
        return token;
    }
}
