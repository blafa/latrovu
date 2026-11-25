using Latrovu.Core.Listings;

namespace Latrovu.Searches;

public interface ISearchStore
{
    Task<IReadOnlyList<Search>> FindBySeekerAsync(string seekerAddress, CancellationToken ct);

    Task SaveAsync(Search search, CancellationToken ct);

    Task SetAreaAsync(Guid searchId, IReadOnlyList<PlaceRef> places, CancellationToken ct);

    Task<IReadOnlyList<Search>> FindMatchesAsync(ListingReadyForMatching listing, CancellationToken ct);

    Task<Search?> FindByIdAsync(Guid searchId, CancellationToken ct);

    Task<bool> RecordAlertAsync(ViewLinkRequested linkRequest, CancellationToken ct);

    Task PublishViewLinkAsync(ViewLinkRequested linkRequest, CancellationToken ct);

    Task SaveCandidateAsync(ListingReadyForMatching listing, CancellationToken ct);

    Task RemoveCandidateAsync(Guid listingId, CancellationToken ct);

    Task RemoveAlertsAsync(Guid listingId, CancellationToken ct);

    Task<IReadOnlyList<ListingReadyForMatching>> FindCandidatesForSearchAsync(Guid searchId, int limit, CancellationToken ct);

    Task<bool> IsProcessedAsync(string sourceId, CancellationToken ct);

    Task MarkProcessedAsync(string sourceId, CancellationToken ct);

    Task RecordSentMessageAsync(string reference, Guid searchId, CancellationToken ct);

    Task<Guid?> FindSearchBySentMessageAsync(string reference, CancellationToken ct);

    Task<int> CountAlertedMatchesAsync(Guid searchId, CancellationToken ct);
}
