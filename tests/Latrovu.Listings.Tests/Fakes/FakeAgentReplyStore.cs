using Latrovu.Listings.Viewing.Chat;

namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeAgentReplyStore : IAgentReplyStore, IAnswerTranslator
{
    public AgentReplyTarget? Target { get; set; }

    public List<string> RecordedAnswers { get; } = [];

    public List<string> RecordedFollowUps { get; } = [];

    public Task<AgentReplyTarget?> FindAsync(string reference, CancellationToken ct) => Task.FromResult(Target);

    public Task<bool> RecordAsync(
        AgentReplyTarget target,
        TranslatedAnswer answer,
        string originalAnswer,
        string sourceMessageId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        RecordedAnswers.Add(answer.Text);
        return Task.FromResult(true);
    }

    public Task<bool> RecordSeekerFollowUpAsync(AgentReplyTarget target, string text, string sourceMessageId, DateTimeOffset now, CancellationToken ct)
    {
        RecordedFollowUps.Add(text);
        return Task.FromResult(true);
    }

    public Task<TranslatedAnswer> PrepareForSeekerAsync(string answer, IReadOnlyList<string> questions, string listingDataJson, CancellationToken ct) =>
        Task.FromResult(new TranslatedAnswer(answer, false));
}
