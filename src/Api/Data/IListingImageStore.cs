namespace Latrovu.Api.Data;

public interface IListingImageStore
{
    Task AddAsync(Guid listingId, int position, string storageKey, string contentType, CancellationToken ct);
}
