namespace Latrovu.Listings.Viewing.Chat;

public interface IAgentReplyStore
{
    Task<AgentReplyTarget?> FindAsync(string reference, CancellationToken ct);

    Task<bool> RecordAsync(
        AgentReplyTarget target,
        TranslatedAnswer answer,
        string originalAnswer,
        string sourceMessageId,
        DateTimeOffset now,
        CancellationToken ct);

    Task<bool> RecordSeekerFollowUpAsync(
        AgentReplyTarget target,
        string text,
        string sourceMessageId,
        DateTimeOffset now,
        CancellationToken ct);
}
