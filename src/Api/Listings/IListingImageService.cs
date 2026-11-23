using Latrovu.Api.Images;

namespace Latrovu.Api.Listings;

public interface IListingImageService
{
    Task AttachAsync(Guid listingId, IReadOnlyList<ImageFile> images, CancellationToken ct);
}
