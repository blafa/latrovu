using Latrovu.Core.Listings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Latrovu.Searches.Tests;

public sealed class ListingAccessRequestedHandlerTests
{
    private const string Address = "friend@example.com";
    private const string Language = "it";
    private const string City = "Thun";

    private static readonly Guid ListingId = Guid.NewGuid();

    private readonly FakeSeekerNotifier notifier = new();

    [Fact]
    public async Task Handle_NewAddress_CreatesPausedSearchAndSendsLink()
    {
        FakeSearchStore store = new();

        await Handler(store).HandleAsync(Request(), CancellationToken.None);

        Search created = Assert.Single(store.Searches);
        Assert.Equal(Search.PausedStatus, created.Status);
        Assert.Equal(City, created.Name);
        ViewLinkRequested link = Assert.Single(store.PublishedLinks);
        Assert.Equal(created.Id, link.SearchId);
        Assert.Equal(Language, Assert.Single(notifier.AccessLinks).Language);
        Assert.Equal(created.Id, store.SentMessages[FakeSeekerNotifier.SentReference]);
    }

    [Fact]
    public async Task Handle_RepeatedRequest_ReusesTheSearch()
    {
        Search existing = new(Guid.NewGuid(), Address, ListingAccessRequestedHandler.EmailChannel, City, Language, "{}", Search.PausedStatus);
        FakeSearchStore store = new(existing);

        await Handler(store).HandleAsync(Request(), CancellationToken.None);

        Assert.Single(store.Searches);
        Assert.Equal(existing.Id, Assert.Single(store.PublishedLinks).SearchId);
    }

    private ListingAccessRequestedHandler Handler(FakeSearchStore store) =>
        new(store, notifier, TimeProvider.System, NullLogger<ListingAccessRequestedHandler>.Instance);

    private static ListingAccessRequested Request() =>
        new(ListingId, Address, Language, "4½-Zimmer-Einfamilienhaus", City);
}
