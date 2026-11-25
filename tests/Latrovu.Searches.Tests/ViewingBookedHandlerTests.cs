using Latrovu.Core.Listings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Latrovu.Searches.Tests;

public sealed class ViewingBookedHandlerTests
{
    private const string Address = "seeker@example.com";
    private const string Channel = "email";
    private const string Language = "fr";

    private static readonly DateTimeOffset StartsAt = new(2026, 12, 5, 13, 0, 0, TimeSpan.Zero);

    private readonly Search search = new(Guid.NewGuid(), Address, Channel, "Lausanne", Language, "{}", Search.ActiveStatus);
    private readonly FakeSeekerNotifier notifier = new();
    private FakeSearchStore store = new();

    [Fact]
    public async Task Handle_KnownSearch_SendsConfirmationInSearchLanguage()
    {
        await Handler(search).HandleAsync(Booked(search.Id), CancellationToken.None);

        SeekerViewingConfirmation sent = Assert.Single(notifier.ViewingConfirmations);
        Assert.Equal(Address, sent.Address);
        Assert.Equal(Language, sent.Language);
        Assert.Equal(StartsAt, sent.StartsAt);
        Assert.Equal(search.Id, store.SentMessages[FakeSeekerNotifier.SentReference]);
    }

    [Fact]
    public async Task Handle_UnknownSearch_SendsNothing()
    {
        await Handler().HandleAsync(Booked(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(notifier.ViewingConfirmations);
    }

    private ViewingBookedHandler Handler(params Search[] searches)
    {
        store = new FakeSearchStore(searches);
        return new ViewingBookedHandler(store, notifier, NullLogger<ViewingBookedHandler>.Instance);
    }

    private static ViewingBooked Booked(Guid searchId) =>
        new(searchId, Guid.NewGuid(), "Appartement 3 pièces", StartsAt, StartsAt.AddMinutes(30));
}
