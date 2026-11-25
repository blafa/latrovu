using Latrovu.Listings.Viewing.Chat;

namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeChatAnswerer : IListingChatAnswerer, IListingSourceTextStore, IListingKnowledgeStore
{
    public ChatAnswer Answer { get; set; } = new("Ja, Haustiere sind erlaubt.", ChatMessage.ExposeSource, 2);

    public List<KnowledgeEntry> Knowledge { get; } = [];

    public IReadOnlyList<KnowledgeEntry> LastKnowledge { get; private set; } = [];

    public Task<ChatAnswer?> AnswerAsync(
        string listingDataJson,
        IReadOnlyList<string> pages,
        IReadOnlyList<KnowledgeEntry> knowledge,
        IReadOnlyList<ChatMessage> history,
        string question,
        CancellationToken ct)
    {
        LastKnowledge = knowledge;
        return Task.FromResult<ChatAnswer?>(Answer);
    }

    public Task<IReadOnlyList<string>> GetPagesAsync(Guid listingId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<string>>(["Seite eins", "Haustiere erlaubt"]);

    public Task<IReadOnlyList<KnowledgeEntry>> GetAsync(Guid listingId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<KnowledgeEntry>>(Knowledge.ToList());

    public Task<bool> RemoveAsync(Guid listingId, long entryId, CancellationToken ct) =>
        Task.FromResult(Knowledge.RemoveAll(entry => entry.Id == entryId) > 0);
}
