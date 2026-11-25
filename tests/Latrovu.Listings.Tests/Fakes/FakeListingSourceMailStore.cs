namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeListingSourceMailStore : IListingSourceMailStore
{
    public List<(Guid ListingId, string MessageId)> Recorded { get; } = [];

    public Task RecordAsync(Guid listingId, string messageId, CancellationToken ct)
    {
        Recorded.Add((listingId, messageId));
        return Task.CompletedTask;
    }
}
