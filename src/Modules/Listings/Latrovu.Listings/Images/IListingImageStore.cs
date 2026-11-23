namespace Latrovu.Listings.Images;

public interface IListingImageStore
{
    Task AddAsync(Guid listingId, int position, string storageKey, string contentType, CancellationToken ct);
}
