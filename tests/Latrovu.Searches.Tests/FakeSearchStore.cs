using Latrovu.Core.Listings;

namespace Latrovu.Searches.Tests;

internal sealed class FakeSearchStore(params Search[] initial) : ISearchStore
{
    private readonly List<Search> searches = [.. initial];

    public Dictionary<string, Guid> SentMessages { get; } = [];

    public List<ViewLinkRequested> PublishedLinks { get; } = [];

    public IReadOnlyList<Search> Searches => searches;

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
        Task.FromResult<IReadOnlyList<Search>>(searches.Where(search => search.SeekerAddress == seekerAddress).ToList());

    public Task SaveAsync(Search search, CancellationToken ct)
    {
        searches.RemoveAll(existing => existing.Id == search.Id);
        searches.Add(search);
        return Task.CompletedTask;
    }

    public Task PublishViewLinkAsync(ViewLinkRequested linkRequest, CancellationToken ct)
    {
        PublishedLinks.Add(linkRequest);
        return Task.CompletedTask;
    }

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

    public Task RemoveAlertsAsync(Guid listingId, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<ListingReadyForMatching>> FindCandidatesForSearchAsync(Guid searchId, int limit, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<bool> IsProcessedAsync(string sourceId, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task MarkProcessedAsync(string sourceId, CancellationToken ct) =>
        throw new NotSupportedException();
}
