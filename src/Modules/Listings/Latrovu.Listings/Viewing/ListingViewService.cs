namespace Latrovu.Listings.Viewing;

public sealed class ListingViewService(
    IListingViewLinkStore links,
    IListingReviewStore listings,
    TimeProvider timeProvider,
    ILogger<ListingViewService> logger) : IListingViewService
{
    public static readonly TimeSpan LinkValidity = TimeSpan.FromDays(30);
    public static readonly TimeSpan SharedLinkValidity = TimeSpan.FromDays(7);

    public async Task<string?> CreateLinkAsync(Guid listingId, Guid? searchId, CancellationToken ct)
    {
        ListingReview? listing = await listings.GetAsync(listingId, ct);
        if (listing is null || !listing.IsApproved)
        {
            logger.LogWarning("No view link created for listing {ListingId}, it is missing or not approved", listingId);
            return null;
        }

        string token = await links.CreateAsync(listingId, searchId, timeProvider.GetUtcNow() + LinkValidity, ct);
        logger.LogInformation("View link created for listing {ListingId} and search {SearchId}", listingId, searchId);
        return token;
    }

    public async Task<string?> ShareAsync(string token, CancellationToken ct)
    {
        (ListingViewLink link, ListingReview listing)? found = await FindLinkAsync(token, ct);
        if (found is null)
        {
            return null;
        }

        ListingViewLink link = found.Value.link;
        if (link.IsShared)
        {
            logger.LogInformation("Shared link for listing {ListingId} passed on again", link.ListingId);
            return token;
        }

        DateTimeOffset latest = timeProvider.GetUtcNow() + SharedLinkValidity;
        string shared = await links.CreateSharedAsync(link.ListingId, link.ExpiresAt < latest ? link.ExpiresAt : latest, ct);
        logger.LogInformation("Shared link created for listing {ListingId}", link.ListingId);
        return shared;
    }

    public async Task<ListingView?> OpenAsync(string token, CancellationToken ct)
    {
        (ListingViewLink link, ListingReview listing)? found = await FindLinkAsync(token, ct);
        if (found is null)
        {
            return null;
        }

        await links.RegisterViewAsync(token, timeProvider.GetUtcNow(), ct);
        return new ListingView(found.Value.listing, found.Value.link.IsShared);
    }

    public async Task<ListingReview?> FindAsync(string token, CancellationToken ct) =>
        (await FindLinkAsync(token, ct))?.listing;

    public async Task<ListingReview?> FindPersonalAsync(string token, CancellationToken ct)
    {
        (ListingViewLink link, ListingReview listing)? found = await FindLinkAsync(token, ct);
        if (found is { link.IsShared: true })
        {
            logger.LogWarning("Action on listing {ListingId} refused for a shared link", found.Value.link.ListingId);
            return null;
        }

        return found?.listing;
    }

    public async Task<ListingReview?> FindSharedAsync(string token, CancellationToken ct)
    {
        (ListingViewLink link, ListingReview listing)? found = await FindLinkAsync(token, ct);
        return found is { link.IsShared: true } ? found.Value.listing : null;
    }

    private async Task<(ListingViewLink link, ListingReview listing)?> FindLinkAsync(string token, CancellationToken ct)
    {
        ListingViewLink? link = await links.FindAsync(token, timeProvider.GetUtcNow(), ct);
        ListingReview? listing = link is null ? null : await listings.GetAsync(link.ListingId, ct);
        return link is not null && listing is { IsApproved: true } ? (link, listing) : null;
    }
}
