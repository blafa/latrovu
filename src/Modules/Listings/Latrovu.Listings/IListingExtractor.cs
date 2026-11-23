namespace Latrovu.Listings;

public interface IListingExtractor
{
    Task<ListingExtraction?> ExtractAsync(string exposeText, CancellationToken ct);
}
