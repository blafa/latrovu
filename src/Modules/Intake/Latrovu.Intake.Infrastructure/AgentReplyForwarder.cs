using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;
using Npgsql;

namespace Latrovu.Intake.Infrastructure;

public sealed class AgentReplyForwarder(
    IOutbox outbox,
    NpgsqlDataSource dataSource,
    ILogger<AgentReplyForwarder> logger) : IAgentReplyForwarder
{
    public async Task ForwardAsync(InboundMail mail, string reference, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mail);

        string text = QuotedReplyStripper.Strip(mail.Body);
        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogWarning("Agent reply {Reference} in mail {MessageId} has no text and is ignored", reference, mail.MessageId);
            return;
        }

        AgentReplyReceived message = new(Guid.NewGuid(), mail.MessageId, reference, mail.Sender, text);
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);
        await outbox.AddAsync([message], transaction, ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation("Agent reply {ReplyId} for {Reference} forwarded", message.ReplyId, reference);
    }
}
