namespace Latrovu.Api.Listings;

public interface IListingExtractor
{
    Task<string?> ExtractAsync(string exposeText, CancellationToken ct);
}
