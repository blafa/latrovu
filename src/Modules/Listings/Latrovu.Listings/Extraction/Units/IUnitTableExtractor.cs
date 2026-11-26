namespace Latrovu.Listings.Extraction.Units;

public interface IUnitTableExtractor
{
    Task<IReadOnlyList<ListingUnit>> ExtractAsync(IReadOnlyList<string> pages, CancellationToken ct);
}
