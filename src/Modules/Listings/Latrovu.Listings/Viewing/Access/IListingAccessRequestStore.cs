using Latrovu.Core.Listings;

namespace Latrovu.Listings.Viewing.Access;

public interface IListingAccessRequestStore
{
    Task<AccessRequestCounts> CountAsync(string tokenHash, string address, DateTimeOffset addressWindowStart, CancellationToken ct);

    Task RecordAsync(string tokenHash, ListingAccessRequested request, DateTimeOffset now, CancellationToken ct);
}
