using Latrovu.Core.Listings;
using Latrovu.Listings.Viewing.Access;

namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeListingAccessRequestStore : IListingAccessRequestStore
{
    private readonly List<(string TokenHash, ListingAccessRequested Request, DateTimeOffset At)> recorded = [];

    public IReadOnlyList<ListingAccessRequested> Requests => recorded.Select(entry => entry.Request).ToList();

    public Task<AccessRequestCounts> CountAsync(string tokenHash, string address, DateTimeOffset addressWindowStart, CancellationToken ct) =>
        Task.FromResult(new AccessRequestCounts(
            recorded.Count(entry => entry.TokenHash == tokenHash),
            recorded.Count(entry => string.Equals(entry.Request.Address, address, StringComparison.OrdinalIgnoreCase) && entry.At > addressWindowStart)));

    public Task RecordAsync(string tokenHash, ListingAccessRequested request, DateTimeOffset now, CancellationToken ct)
    {
        recorded.Add((tokenHash, request, now));
        return Task.CompletedTask;
    }
}
