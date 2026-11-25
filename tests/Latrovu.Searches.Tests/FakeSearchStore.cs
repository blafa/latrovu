using Latrovu.Core.Listings;

namespace Latrovu.Searches.Tests;

internal sealed class FakeSearchStore(params Search[] searches) : ISearchStore
{
    public Dictionary<string, Guid> SentMessages { get; } = [];

    public Task RecordSentMessageAsync(string reference, Guid searchId, CancellationToken ct)
    {
        SentMessages[reference] = searchId;
        return Task.CompletedTask;
    }

    public Task<Guid?> FindSearchBySentMessageAsync(string reference, CancellationToken ct) =>
        Task.FromResult<Guid?>(SentMessages.TryGetValue(reference, out Guid searchId) ? searchId : null);

    public Task<int> CountAlertedMatchesAsync(Guid searchId, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<Search?> FindByIdAsync(Guid searchId, CancellationToken ct) =>
        Task.FromResult(searches.FirstOrDefault(search => search.Id == searchId));

    public Task<IReadOnlyList<Search>> FindBySeekerAsync(string seekerAddress, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task SaveAsync(Search search, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task SetAreaAsync(Guid searchId, IReadOnlyList<PlaceRef> places, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<Search>> FindMatchesAsync(ListingReadyForMatching listing, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<bool> RecordAlertAsync(ViewLinkRequested linkRequest, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task SaveCandidateAsync(ListingReadyForMatching listing, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task RemoveCandidateAsync(Guid listingId, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<ListingReadyForMatching>> FindCandidatesForSearchAsync(Guid searchId, int limit, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<bool> IsProcessedAsync(string sourceId, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task MarkProcessedAsync(string sourceId, CancellationToken ct) =>
        throw new NotSupportedException();
}
