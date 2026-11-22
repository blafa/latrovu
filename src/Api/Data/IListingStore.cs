using Latrovu.Api.Listings;

namespace Latrovu.Api.Data;

public interface IListingStore
{
    Task<Guid> AddAsync(NewListing listing, CancellationToken ct);
}
