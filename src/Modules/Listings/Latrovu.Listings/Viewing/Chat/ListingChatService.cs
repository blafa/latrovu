namespace Latrovu.Listings.Viewing.Chat;

public sealed class ListingChatService(
    IListingViewService views,
    IListingChatStore chats,
    IListingSourceTextStore sourceTexts,
    IListingChatAnswerer answerer,
    TimeProvider timeProvider,
    ILogger<ListingChatService> logger)
{
    public const int MaxQuestionsPerDay = 20;
    public const int MaxQuestionLength = 1000;
    private static readonly TimeSpan LimitWindow = TimeSpan.FromDays(1);

    public async Task<ChatResult> GetAsync(string token, CancellationToken ct) =>
        await views.FindAsync(token, ct) is null
            ? new ChatResult(ChatOutcome.NotFound, [])
            : new ChatResult(ChatOutcome.Answered, await chats.GetMessagesAsync(token, ct));

    public async Task<ChatResult> AskAsync(string token, string question, CancellationToken ct)
    {
        ListingReview? listing = await views.FindAsync(token, ct);
        if (listing is null)
        {
            return new ChatResult(ChatOutcome.NotFound, []);
        }

        IReadOnlyList<ChatMessage> history = await chats.GetMessagesAsync(token, ct);
        ChatOutcome? rejection = await RejectAsync(token, question, ct);
        if (rejection is not null)
        {
            return new ChatResult(rejection.Value, history);
        }

        ChatAnswer answer = await AnswerAsync(listing, history, question.Trim(), ct);
        await chats.AddAsync(token, listing.Id, new ChatMessage(ChatMessage.SeekerRole, question.Trim(), null, null, timeProvider.GetUtcNow()), ct);
        await chats.AddAsync(token, listing.Id, new ChatMessage(ChatMessage.AssistantRole, answer.Text, answer.Source, answer.Page, timeProvider.GetUtcNow()), ct);
        if (answer.Source == ChatMessage.RequestSource)
        {
            await ForwardRequestAsync(token, question.Trim(), ct);
        }

        logger.LogInformation("Chat question for listing {ListingId} answered from {Source}", listing.Id, answer.Source);
        return new ChatResult(ChatOutcome.Answered, await chats.GetMessagesAsync(token, ct));
    }

    private async Task ForwardRequestAsync(string token, string request, CancellationToken ct)
    {
        IReadOnlyList<ChatMessage> messages = await chats.GetMessagesAsync(token, ct);
        if (messages.LastOrDefault() is { Role: ChatMessage.AssistantRole } confirmation)
        {
            await chats.MarkForwardAsync(token, confirmation.Id, request, timeProvider.GetUtcNow(), ct);
        }
    }

    public async Task<ChatResult> ForwardAsync(string token, long answerId, CancellationToken ct)
    {
        ListingReview? listing = await views.FindAsync(token, ct);
        if (listing is null)
        {
            return new ChatResult(ChatOutcome.NotFound, []);
        }

        IReadOnlyList<ChatMessage> messages = await chats.GetMessagesAsync(token, ct);
        int index = messages.ToList().FindIndex(message => message.Id == answerId && message.CanBeForwarded);
        ChatMessage? question = index > 0 && messages[index - 1].Role == ChatMessage.SeekerRole ? messages[index - 1] : null;
        if (question is null || !await chats.MarkForwardAsync(token, answerId, question.Text, timeProvider.GetUtcNow(), ct))
        {
            return new ChatResult(ChatOutcome.InvalidQuestion, messages);
        }

        logger.LogInformation("Chat question for listing {ListingId} marked for forwarding", listing.Id);
        return new ChatResult(ChatOutcome.Answered, await chats.GetMessagesAsync(token, ct));
    }

    private async Task<ChatOutcome?> RejectAsync(string token, string question, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(question) || question.Length > MaxQuestionLength)
        {
            return ChatOutcome.InvalidQuestion;
        }

        int asked = await chats.CountSeekerMessagesSinceAsync(token, timeProvider.GetUtcNow() - LimitWindow, ct);
        return asked >= MaxQuestionsPerDay ? ChatOutcome.LimitReached : null;
    }

    private async Task<ChatAnswer> AnswerAsync(ListingReview listing, IReadOnlyList<ChatMessage> history, string question, CancellationToken ct)
    {
        IReadOnlyList<string> pages = await sourceTexts.GetPagesAsync(listing.Id, ct);
        ChatAnswer? answer = await answerer.AnswerAsync(listing.DataJson, pages, history, question, ct);
        return answer ?? new ChatAnswer(string.Empty, ChatMessage.UnknownSource, null);
    }
}
