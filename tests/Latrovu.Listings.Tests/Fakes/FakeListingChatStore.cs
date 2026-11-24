using Latrovu.Listings.Viewing.Chat;

namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeListingChatStore : IListingChatStore
{
    private readonly List<ChatMessage> messages = [];

    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string token, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ChatMessage>>(messages.ToList());

    public Task AddAsync(string token, Guid listingId, ChatMessage message, CancellationToken ct)
    {
        messages.Add(message with { Id = messages.Count + 1 });
        return Task.CompletedTask;
    }

    public Task<int> CountSeekerMessagesSinceAsync(string token, DateTimeOffset since, CancellationToken ct) =>
        Task.FromResult(messages.Count(message => message.Role == ChatMessage.SeekerRole && message.CreatedAt > since));

    public Task<bool> MarkForwardAsync(string token, long answerId, string question, DateTimeOffset now, CancellationToken ct)
    {
        int index = messages.FindIndex(message => message.Id == answerId && message.ForwardStatus is null);
        if (index < 0)
        {
            return Task.FromResult(false);
        }

        messages[index] = messages[index] with { ForwardStatus = ChatMessage.ForwardPending };
        return Task.FromResult(true);
    }
}
