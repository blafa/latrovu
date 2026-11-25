using Latrovu.BuildingBlocks.Storage;

namespace Latrovu.Listings.Review;

public sealed class ListingRemover(
    IListingReviewStore reviews,
    IObjectStorage storage,
    ILogger<ListingRemover> logger)
{
    public async Task<bool> RemoveAsync(Guid listingId, CancellationToken ct)
    {
        WithdrawnListing? withdrawn = await reviews.WithdrawAsync(listingId, ct);
        if (withdrawn is null)
        {
            logger.LogWarning("Listing {ListingId} not removed, it is missing or not approved", listingId);
            return false;
        }

        await DeleteImagesAsync(listingId, withdrawn.ImageKeys, ct);
        logger.LogInformation("Listing {ListingId} withdrawn and deleted with {ImageCount} images", listingId, withdrawn.ImageKeys.Count);
        return true;
    }

    private async Task DeleteImagesAsync(Guid listingId, IReadOnlyList<string> imageKeys, CancellationToken ct)
    {
        foreach (string key in imageKeys)
        {
            try
            {
                await storage.DeleteAsync(key, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or IOException)
            {
                logger.LogError(ex, "Image {StorageKey} of deleted listing {ListingId} could not be removed from storage", key, listingId);
            }
        }
    }
}
