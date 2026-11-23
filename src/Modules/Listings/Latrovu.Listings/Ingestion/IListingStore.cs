
namespace Latrovu.Listings.Ingestion;

public interface IListingStore
{
    Task<Guid> AddAsync(NewListing listing, CancellationToken ct);
}
