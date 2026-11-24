using Latrovu.Listings.Viewing.Chat;
using Microsoft.Extensions.Logging.Abstractions;

namespace Latrovu.Listings.Tests.Viewing;

public sealed class ListingChatServiceTests
{
    private static readonly Guid ListingId = Guid.NewGuid();

    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly FakeListingReviewStore listings = new();
    private readonly FakeListingViewLinkStore links = new();
    private readonly FakeListingChatStore chats = new();
    private readonly FakeChatAnswerer answerer = new();
    private readonly ListingChatService service;
    private readonly ListingViewService views;

    public ListingChatServiceTests()
    {
        views = new ListingViewService(links, listings, time, NullLogger<ListingViewService>.Instance);
        service = new ListingChatService(views, chats, answerer, answerer, time, NullLogger<ListingChatService>.Instance);
    }

    [Fact]
    public async Task Ask_ApprovedListing_StoresQuestionAndAnswer()
    {
        string token = await ApprovedLinkAsync();

        ChatResult result = await service.AskAsync(token, "Sind Haustiere erlaubt?", CancellationToken.None);

        Assert.Equal(ChatOutcome.Answered, result.Outcome);
        Assert.Equal(2, result.Messages.Count);
        Assert.Equal(ChatMessage.ExposeSource, result.Messages[1].Source);
    }

    [Fact]
    public async Task Ask_AfterDailyLimit_ReturnsLimitReached()
    {
        string token = await ApprovedLinkAsync();
        for (int question = 0; question < ListingChatService.MaxQuestionsPerDay; question++)
        {
            await service.AskAsync(token, $"Frage {question}", CancellationToken.None);
        }

        ChatResult result = await service.AskAsync(token, "Noch eine Frage", CancellationToken.None);

        Assert.Equal(ChatOutcome.LimitReached, result.Outcome);
    }

    [Fact]
    public async Task Ask_BlankQuestion_IsInvalid()
    {
        string token = await ApprovedLinkAsync();

        ChatResult result = await service.AskAsync(token, "   ", CancellationToken.None);

        Assert.Equal(ChatOutcome.InvalidQuestion, result.Outcome);
    }

    [Fact]
    public async Task Ask_UnknownLink_ReturnsNotFound()
    {
        ChatResult result = await service.AskAsync("unknown", "Frage", CancellationToken.None);

        Assert.Equal(ChatOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public async Task Forward_UnknownAnswer_MarksItPendingOnce()
    {
        string token = await ApprovedLinkAsync();
        answerer.Answer = new ChatAnswer("Das steht nicht im Exposé.", ChatMessage.UnknownSource, null);
        ChatResult asked = await service.AskAsync(token, "Sind Haustiere erlaubt?", CancellationToken.None);
        long answerId = asked.Messages[1].Id;

        ChatResult forwarded = await service.ForwardAsync(token, answerId, CancellationToken.None);
        ChatResult again = await service.ForwardAsync(token, answerId, CancellationToken.None);

        Assert.Equal(ChatMessage.ForwardPending, forwarded.Messages[1].ForwardStatus);
        Assert.Equal(ChatOutcome.InvalidQuestion, again.Outcome);
    }

    [Fact]
    public async Task Forward_AnsweredFromExpose_IsRejected()
    {
        string token = await ApprovedLinkAsync();
        ChatResult asked = await service.AskAsync(token, "Hat es einen Garten?", CancellationToken.None);

        ChatResult result = await service.ForwardAsync(token, asked.Messages[1].Id, CancellationToken.None);

        Assert.Equal(ChatOutcome.InvalidQuestion, result.Outcome);
    }

    [Fact]
    public async Task Ask_RequestToAct_IsForwardedRightAway()
    {
        string token = await ApprovedLinkAsync();
        answerer.Answer = new ChatAnswer("Wir haben Ihre Anfrage weitergeleitet.", ChatMessage.RequestSource, null);

        ChatResult result = await service.AskAsync(token, "Bitte vereinbaren Sie eine Besichtigung", CancellationToken.None);

        Assert.Equal(ChatMessage.ForwardPending, result.Messages[1].ForwardStatus);
    }

    private async Task<string> ApprovedLinkAsync()
    {
        await listings.ApproveAsync(ListingId, CancellationToken.None);
        return (await views.CreateLinkAsync(ListingId, searchId: null, CancellationToken.None))!;
    }
}
