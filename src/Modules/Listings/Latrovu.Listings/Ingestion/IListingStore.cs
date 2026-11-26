namespace Latrovu.Listings.Ingestion;

public interface IListingStore
{
    Task<Guid> AddAsync(NewListing listing, CancellationToken ct);

    Task<Guid> AddProjectAsync(NewProject project, CancellationToken ct);

    Task<Guid?> FindActiveBySourceAsync(string sourceEmail, string sourceHash, CancellationToken ct);

    Task FillMissingSourcePagesAsync(Guid listingId, IReadOnlyList<string> pages, CancellationToken ct);
}
