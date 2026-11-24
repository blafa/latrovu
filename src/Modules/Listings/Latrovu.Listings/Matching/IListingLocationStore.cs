using Latrovu.BuildingBlocks.Geo;
using Latrovu.Core.Listings;

namespace Latrovu.Listings.Matching;

public interface IListingLocationStore
{
    Task SaveAsync(Guid listingId, GeoPoint point, ListingReadyForMatching announcement, CancellationToken ct);
}
