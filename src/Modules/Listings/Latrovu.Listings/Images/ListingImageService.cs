using System.Globalization;
using Latrovu.BuildingBlocks.Images;
using Latrovu.BuildingBlocks.Storage;

namespace Latrovu.Listings.Images;

public sealed class ListingImageService(
    IObjectStorage storage,
    IListingImageStore imageStore,
    ILogger<ListingImageService> logger) : IListingImageService
{
    private const string StorageKeyFormat = "listings/{0}/{1:D2}{2}";

    public async Task AttachAsync(Guid listingId, IReadOnlyList<ImageFile> images, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(images);

        for (int index = 0; index < images.Count; index++)
        {
            int position = index + 1;
            ImageFile image = images[index];
            string key = string.Format(CultureInfo.InvariantCulture, StorageKeyFormat, listingId, position, image.Extension);
            await storage.PutAsync(key, image.Content, image.ContentType, ct);
            await imageStore.AddAsync(listingId, position, key, image.ContentType, ct);
        }

        logger.LogInformation("Attached {ImageCount} images to listing {ListingId}", images.Count, listingId);
    }
}
