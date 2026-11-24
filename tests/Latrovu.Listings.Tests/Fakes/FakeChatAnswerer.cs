using Latrovu.Listings.Viewing.Chat;

namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeChatAnswerer : IListingChatAnswerer, IListingSourceTextStore
{
    public ChatAnswer Answer { get; set; } = new("Ja, Haustiere sind erlaubt.", ChatMessage.ExposeSource, 2);

    public Task<ChatAnswer?> AnswerAsync(
        string listingDataJson,
        IReadOnlyList<string> pages,
        IReadOnlyList<ChatMessage> history,
        string question,
        CancellationToken ct) => Task.FromResult<ChatAnswer?>(Answer);

    public Task<IReadOnlyList<string>> GetPagesAsync(Guid listingId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>(["Seite eins", "Haustiere erlaubt"]);
}
