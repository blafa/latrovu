using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;

namespace Latrovu.Listings.Viewing.Chat;

public sealed class AgentReplyHandler(
    IAgentReplyStore replies,
    IAnswerTranslator translator,
    TimeProvider timeProvider,
    ILogger<AgentReplyHandler> logger) : IMessageHandler<AgentReplyReceived>
{
    public async Task HandleAsync(AgentReplyReceived message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        AgentReplyTarget? target = await replies.FindAsync(message.Reference, ct);
        if (target is null)
        {
            logger.LogWarning("Reply {ReplyId} references unknown {Reference}, ignored", message.ReplyId, message.Reference);
            return;
        }

        if (IsFrom(message, target.AgentAddress))
        {
            await RecordAgentAnswerAsync(message, target, ct);
            return;
        }

        if (target.SeekerAddress is { } seeker && IsFrom(message, seeker))
        {
            bool recorded = await replies.RecordSeekerFollowUpAsync(target, message.Text.Trim(), message.SourceMessageId, timeProvider.GetUtcNow(), ct);
            logger.LogInformation("Seeker follow-up {ReplyId} for listing {ListingId} {Outcome}", message.ReplyId, target.ListingId, Outcome(recorded));
            return;
        }

        logger.LogWarning("Reply {ReplyId} for {Reference} came from neither the listing's nor the seeker's address, ignored", message.ReplyId, message.Reference);
    }

    private async Task RecordAgentAnswerAsync(AgentReplyReceived message, AgentReplyTarget target, CancellationToken ct)
    {
        string original = message.Text.Trim();
        TranslatedAnswer prepared = await translator.PrepareForSeekerAsync(original, target.Questions, target.ListingDataJson, ct);
        bool recorded = await replies.RecordAsync(target, prepared, original, message.SourceMessageId, timeProvider.GetUtcNow(), ct);
        logger.LogInformation(
            "Agent reply {ReplyId} for listing {ListingId} {Outcome}, translated: {Translated}",
            message.ReplyId,
            target.ListingId,
            Outcome(recorded),
            prepared.Translated);
    }

    private static bool IsFrom(AgentReplyReceived message, string address) =>
        string.Equals(address.Trim(), message.SenderAddress.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string Outcome(bool recorded) => recorded ? "added to the conversation" : "was already processed";
}
