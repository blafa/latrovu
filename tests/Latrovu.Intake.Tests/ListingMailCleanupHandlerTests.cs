using Latrovu.Core.Listings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Latrovu.Intake.Tests;

public sealed class ListingMailCleanupHandlerTests
{
    private readonly FakeMailInbox inbox = new();
    private readonly ListingMailCleanupHandler handler;

    public ListingMailCleanupHandlerTests()
    {
        handler = new ListingMailCleanupHandler(inbox, NullLogger<ListingMailCleanupHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithdrawnListing_DeletesItsMails()
    {
        await handler.HandleAsync(new ListingWithdrawn(Guid.NewGuid(), ["expose@agency.ch", "reply@agency.ch"]), CancellationToken.None);

        Assert.Equal(["expose@agency.ch", "reply@agency.ch"], inbox.DeletedMessageIds);
    }

    [Fact]
    public async Task Handle_OldEventWithoutMails_DoesNotTouchTheMailbox()
    {
        await handler.HandleAsync(new ListingWithdrawn(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(0, inbox.Calls);
    }
}
