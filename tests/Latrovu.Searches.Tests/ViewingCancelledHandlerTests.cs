using Latrovu.Core.Listings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Latrovu.Searches.Tests;

public sealed class ViewingCancelledHandlerTests
{
    private static readonly DateTimeOffset StartsAt = new(2026, 12, 5, 13, 0, 0, TimeSpan.Zero);

    private readonly Search search = new(Guid.NewGuid(), "seeker@example.com", "email", "Thun", "it", "{}", Search.ActiveStatus);
    private readonly FakeSeekerNotifier notifier = new();

    [Fact]
    public async Task Handle_KnownSearch_SendsCancellationInSearchLanguage()
    {
        FakeSearchStore store = new(search);
        ViewingCancelledHandler handler = new(store, notifier, NullLogger<ViewingCancelledHandler>.Instance);

        await handler.HandleAsync(new ViewingCancelled(search.Id, Guid.NewGuid(), "Casa a Thun", StartsAt, StartsAt.AddMinutes(30)), CancellationToken.None);

        SeekerViewingConfirmation sent = Assert.Single(notifier.ViewingConfirmations);
        Assert.True(sent.IsCancellation);
        Assert.Equal("it", sent.Language);
        Assert.Equal(search.Id, store.SentMessages[FakeSeekerNotifier.SentReference]);
    }
}
