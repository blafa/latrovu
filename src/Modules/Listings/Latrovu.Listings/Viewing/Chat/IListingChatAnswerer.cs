namespace Latrovu.Listings.Viewing.Chat;

public interface IListingChatAnswerer
{
    Task<ChatAnswer?> AnswerAsync(
        string listingDataJson,
        IReadOnlyList<string> pages,
        IReadOnlyList<KnowledgeEntry> knowledge,
        IReadOnlyList<ChatMessage> history,
        string question,
        CancellationToken ct);
}
