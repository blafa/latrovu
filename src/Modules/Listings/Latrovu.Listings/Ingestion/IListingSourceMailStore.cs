namespace Latrovu.Listings.Ingestion;

public interface IListingSourceMailStore
{
    Task RecordAsync(Guid listingId, string messageId, CancellationToken ct);
}
