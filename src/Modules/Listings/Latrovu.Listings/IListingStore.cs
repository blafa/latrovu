
namespace Latrovu.Listings;

public interface IListingStore
{
    Task<Guid> AddAsync(NewListing listing, CancellationToken ct);
}
