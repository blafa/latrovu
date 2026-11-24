namespace Latrovu.Listings.Viewing.Chat;

public interface IListingChatStore
{
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string token, CancellationToken ct);

    Task AddAsync(string token, Guid listingId, ChatMessage message, CancellationToken ct);

    Task<int> CountSeekerMessagesSinceAsync(string token, DateTimeOffset since, CancellationToken ct);

    Task<bool> MarkForwardAsync(string token, long answerId, string question, DateTimeOffset now, CancellationToken ct);
}
