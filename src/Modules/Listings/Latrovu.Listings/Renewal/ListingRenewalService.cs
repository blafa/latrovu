namespace Latrovu.Listings.Renewal;

public sealed class ListingRenewalService(
    IListingRenewalStore renewals,
    TimeProvider timeProvider,
    ILogger<ListingRenewalService> logger)
{
    public async Task<bool> IsValidAsync(string token, CancellationToken ct) =>
        await renewals.FindListingByTokenAsync(token, timeProvider.GetUtcNow(), ct) is not null;

    public async Task<bool> RenewAsync(string token, CancellationToken ct)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        Guid? listingId = await renewals.FindListingByTokenAsync(token, now, ct);
        bool renewed = listingId is not null && await renewals.RenewAsync(listingId.Value, now, ct);
        logger.LogInformation("Renewal of listing {ListingId}: {Renewed}", listingId, renewed);
        return renewed;
    }
}
