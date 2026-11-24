namespace Latrovu.Listings.Viewing;

public sealed class ListingViewService(
    IListingViewLinkStore links,
    IListingReviewStore listings,
    TimeProvider timeProvider,
    ILogger<ListingViewService> logger) : IListingViewService
{
    public static readonly TimeSpan LinkValidity = TimeSpan.FromDays(30);

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

    public async Task<ListingReview?> OpenAsync(string token, CancellationToken ct)
    {
        ListingReview? listing = await FindAsync(token, ct);
        if (listing is not null)
        {
            await links.RegisterViewAsync(token, timeProvider.GetUtcNow(), ct);
        }

        return listing;
    }

    public async Task<ListingReview?> FindAsync(string token, CancellationToken ct)
    {
        Guid? listingId = await links.FindListingIdAsync(token, timeProvider.GetUtcNow(), ct);
        ListingReview? listing = listingId is null ? null : await listings.GetAsync(listingId.Value, ct);
        return listing is { IsApproved: true } ? listing : null;
    }
}
