namespace Latrovu.Listings.Extraction;

public interface IListingExtractor
{
    Task<ListingExtraction?> ExtractAsync(IReadOnlyList<string> pages, CancellationToken ct);
}
