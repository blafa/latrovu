using Latrovu.BuildingBlocks.Images;

namespace Latrovu.Listings.Images;

public interface IListingImageService
{
    Task AttachAsync(Guid listingId, IReadOnlyList<ImageFile> images, CancellationToken ct);
}
