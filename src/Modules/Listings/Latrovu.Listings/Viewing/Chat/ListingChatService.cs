using Latrovu.Listings.Viewing.Viewings;

namespace Latrovu.Listings.Viewing.Chat;

public sealed class ListingChatService(
    IListingViewService views,
    IListingChatStore chats,
    IListingSourceTextStore sourceTexts,
    IListingKnowledgeStore knowledge,
    IViewingStore viewings,
    IViewingNotifier notifier,
    IListingChatAnswerer answerer,
    TimeProvider timeProvider,
    ILogger<ListingChatService> logger)
{
    public const int MaxQuestionsPerDay = 20;
    public const int MaxQuestionLength = 1000;
    private const string ViewingQuestion = "Wann sind Besichtigungen möglich?";
    private const string SlotFull = "Dieser Termin ist leider schon voll. Bitte wählen Sie einen anderen.";
    private const string SlotGone = "Diesen Termin gibt es nicht mehr. Bitte wählen Sie einen der aktuellen Termine.";
    private const string NoSlotsForwarded = "Zurzeit sind keine Besichtigungstermine offen. Wir haben Ihren Wunsch an den Anbieter weitergeleitet.";
    private static readonly TimeSpan LimitWindow = TimeSpan.FromDays(1);

    public async Task<ChatResult> GetAsync(string token, CancellationToken ct)
    {
        ListingReview? listing = await views.FindAsync(token, ct);
        return listing is null
            ? new ChatResult(ChatOutcome.NotFound, [])
            : await ResultAsync(ChatOutcome.Answered, token, listing.Id, ct);
    }

    public async Task<ChatResult> AskAsync(string token, string question, CancellationToken ct)
    {
        ListingReview? listing = await views.FindAsync(token, ct);
        if (listing is null)
        {
            return new ChatResult(ChatOutcome.NotFound, []);
        }

        ChatOutcome? rejection = await RejectAsync(token, question, ct);
        if (rejection is not null)
        {
            return await ResultAsync(rejection.Value, token, listing.Id, ct);
        }

        IReadOnlyList<ChatMessage> history = await chats.GetMessagesAsync(token, ct);
        ChatAnswer answer = await AnswerAsync(listing, history, question.Trim(), ct);
        await chats.AddAsync(token, listing.Id, new ChatMessage(ChatMessage.SeekerRole, question.Trim(), null, null, timeProvider.GetUtcNow()), ct);
        ChatAnswer final = answer switch
        {
            { Source: ChatMessage.BookingSource, SlotId: { } slotId } => await BookAsync(token, listing.Id, slotId, answer, ct),
            { Source: ChatMessage.ViewingsSource } => await ViewingAnswerAsync(listing.Id, answer, ct),
            _ => answer
        };
        await chats.AddAsync(token, listing.Id, new ChatMessage(ChatMessage.AssistantRole, final.Text, final.Source, final.Page, timeProvider.GetUtcNow()), ct);
        if (final.Source == ChatMessage.RequestSource)
        {
            await ForwardRequestAsync(token, question.Trim(), ct);
        }

        logger.LogInformation("Chat question for listing {ListingId} answered from {Source}", listing.Id, final.Source);
        return await ResultAsync(ChatOutcome.Answered, token, listing.Id, ct);
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
            return await ResultAsync(ChatOutcome.InvalidQuestion, token, listing.Id, ct);
        }

        logger.LogInformation("Chat question for listing {ListingId} marked for forwarding", listing.Id);
        return await ResultAsync(ChatOutcome.Answered, token, listing.Id, ct);
    }

    private async Task<ChatAnswer> ViewingAnswerAsync(Guid listingId, ChatAnswer answer, CancellationToken ct)
    {
        List<ViewingSlot> free = (await viewings.GetSlotsAsync(listingId, null, timeProvider.GetUtcNow(), ct))
            .Where(slot => slot.FreePlaces > 0)
            .ToList();
        if (free.Count == 0)
        {
            return new ChatAnswer(NoSlotsForwarded, ChatMessage.RequestSource, null);
        }

        string text = string.IsNullOrWhiteSpace(answer.Text)
            ? $"Freie Besichtigungstermine: {string.Join(", ", free.Select(SwissTime.Describe))}. Wählen Sie unten einen Termin oder schreiben Sie, welcher passt."
            : answer.Text;
        return new ChatAnswer(text, ChatMessage.ViewingsSource, null);
    }

    private async Task<ChatAnswer> BookAsync(string token, Guid listingId, long slotId, ChatAnswer answer, CancellationToken ct)
    {
        string tokenHash = AccessTokens.Hash(token);
        BookingOutcome outcome = await viewings.BookAsync(listingId, slotId, tokenHash, timeProvider.GetUtcNow(), ct);
        logger.LogInformation("Viewing slot {SlotId} for listing {ListingId} booked from chat: {Outcome}", slotId, listingId, outcome);
        if (outcome != BookingOutcome.Booked)
        {
            return new ChatAnswer(outcome == BookingOutcome.Full ? SlotFull : SlotGone, ChatMessage.ViewingsSource, null);
        }

        IReadOnlyList<ViewingSlot> slots = await viewings.GetSlotsAsync(listingId, tokenHash, timeProvider.GetUtcNow(), ct);
        if (slots.FirstOrDefault(slot => slot.Id == slotId) is { } booked)
        {
            await notifier.NotifyAgentOfBookingAsync(listingId, booked, tokenHash, ct);
        }

        return answer with { SlotId = null };
    }

    private async Task<ChatResult> ResultAsync(ChatOutcome outcome, string token, Guid listingId, CancellationToken ct) =>
        new(outcome, await chats.GetMessagesAsync(token, ct))
        {
            Slots = await viewings.GetSlotsAsync(listingId, AccessTokens.Hash(token), timeProvider.GetUtcNow(), ct)
        };

    private async Task ForwardRequestAsync(string token, string request, CancellationToken ct)
    {
        IReadOnlyList<ChatMessage> messages = await chats.GetMessagesAsync(token, ct);
        if (messages.LastOrDefault() is { Role: ChatMessage.AssistantRole } confirmation)
        {
            await chats.MarkForwardAsync(token, confirmation.Id, request, timeProvider.GetUtcNow(), ct);
        }
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
        IReadOnlyList<KnowledgeEntry> answers = await KnowledgeAsync(listing.Id, ct);
        ChatAnswer? answer = await answerer.AnswerAsync(listing.DataJson, pages, answers, history, question, ct);
        return answer ?? new ChatAnswer(string.Empty, ChatMessage.UnknownSource, null);
    }

    private async Task<IReadOnlyList<KnowledgeEntry>> KnowledgeAsync(Guid listingId, CancellationToken ct)
    {
        IReadOnlyList<KnowledgeEntry> stored = await knowledge.GetAsync(listingId, ct);
        DateTimeOffset now = timeProvider.GetUtcNow();
        IReadOnlyList<ViewingSlot> slots = await viewings.GetSlotsAsync(listingId, null, now, ct);
        return slots.Count == 0 ? stored : [.. stored, SlotsEntry(slots, now)];
    }

    private static KnowledgeEntry SlotsEntry(IReadOnlyList<ViewingSlot> slots, DateTimeOffset now)
    {
        List<string> free = slots.Where(slot => slot.FreePlaces > 0)
            .Select(slot => $"#{slot.Id} {SwissTime.Describe(slot)} ({slot.FreePlaces} frei)")
            .ToList();
        string answer = free.Count == 0
            ? "Alle Besichtigungstermine sind ausgebucht."
            : $"Freie Besichtigungstermine (Nummer, Zeit, freie Plätze): {string.Join("; ", free)}. Buchung direkt hier im Chat oder im Bereich «Besichtigung».";
        return new KnowledgeEntry(0, ViewingQuestion, answer, now);
    }
}
