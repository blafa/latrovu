using Latrovu.Core.Listings;
using Latrovu.Listings.Viewing.Chat;
using Microsoft.Extensions.Logging.Abstractions;

namespace Latrovu.Listings.Tests.Viewing;

public sealed class AgentReplyHandlerTests
{
    private const string AgentAddress = "agent@example.ch";
    private const string SeekerAddress = "seeker@example.ch";
    private const string Reference = "Q-1a2b3c4d5e";

    private readonly FakeAgentReplyStore store = new();
    private readonly FakeListingSourceMailStore mails = new();
    private readonly AgentReplyHandler handler;

    public AgentReplyHandlerTests()
    {
        handler = new AgentReplyHandler(store, store, mails, TimeProvider.System, NullLogger<AgentReplyHandler>.Instance);
        store.Target = new AgentReplyTarget(Guid.NewGuid(), Guid.NewGuid(), "hash", AgentAddress, "Haus in Thun", Guid.NewGuid(), ["Haustiere?"], "{}", SeekerAddress);
    }

    [Fact]
    public async Task Handle_KnownReference_RecordsTheMailForDeletionWithTheListing()
    {
        await handler.HandleAsync(Reply(AgentAddress), CancellationToken.None);

        Assert.Equal(store.Target!.ListingId, Assert.Single(mails.Recorded).ListingId);
    }

    [Fact]
    public async Task Handle_ReplyFromListingSender_IsRecordedAsAnswer()
    {
        await handler.HandleAsync(Reply("AGENT@example.ch"), CancellationToken.None);

        Assert.Equal(["Ja, Katzen sind erlaubt."], store.RecordedAnswers);
        Assert.Empty(store.RecordedFollowUps);
    }

    [Fact]
    public async Task Handle_ReplyFromSeeker_IsRecordedAsFollowUp()
    {
        await handler.HandleAsync(Reply(SeekerAddress), CancellationToken.None);

        Assert.Equal(["Ja, Katzen sind erlaubt."], store.RecordedFollowUps);
        Assert.Empty(store.RecordedAnswers);
    }

    [Fact]
    public async Task Handle_ReplyFromOtherSender_IsIgnored()
    {
        await handler.HandleAsync(Reply("someone@else.ch"), CancellationToken.None);

        Assert.Empty(store.RecordedAnswers);
        Assert.Empty(store.RecordedFollowUps);
    }

    [Fact]
    public async Task Handle_UnknownReference_IsIgnored()
    {
        store.Target = null;

        await handler.HandleAsync(Reply(AgentAddress), CancellationToken.None);

        Assert.Empty(store.RecordedAnswers);
    }

    private static AgentReplyReceived Reply(string sender) =>
        new(Guid.NewGuid(), "message-1", Reference, sender, "  Ja, Katzen sind erlaubt.  ");
}
