namespace Latrovu.Listings.Extraction;

public interface IListingExtractor
{
    Task<ListingExtraction?> ExtractAsync(string exposeText, CancellationToken ct);
}
