namespace Latrovu.Listings.Viewing.Chat;

public interface IListingKnowledgeStore
{
    Task<IReadOnlyList<KnowledgeEntry>> GetAsync(Guid listingId, CancellationToken ct);

    Task<bool> RemoveAsync(Guid listingId, long entryId, CancellationToken ct);
}
