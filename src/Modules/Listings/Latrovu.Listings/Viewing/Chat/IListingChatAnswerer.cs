namespace Latrovu.Listings.Viewing.Chat;

public interface IListingChatAnswerer
{
    Task<ChatAnswer?> AnswerAsync(
        string listingDataJson,
        IReadOnlyList<string> pages,
        IReadOnlyList<ChatMessage> history,
        string question,
        CancellationToken ct);
}
