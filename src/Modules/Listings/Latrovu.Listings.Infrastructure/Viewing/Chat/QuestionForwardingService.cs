using Latrovu.BuildingBlocks.Email;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Latrovu.Listings.Infrastructure.Viewing.Chat;

public sealed class QuestionForwardingService(
    NpgsqlDataSource dataSource,
    IEmailSender emailSender,
    IOptions<QuestionForwardingOptions> options,
    TimeProvider timeProvider,
    ILogger<QuestionForwardingService> logger) : BackgroundService
{
    private const int MaxConversationsPerRun = 20;
    private const string FallbackTitle = "Ihr Inserat";
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private const string DueSql = """
        SELECT token_hash FROM listing_chat_messages WHERE forward_status = 'pending'
        GROUP BY token_hash HAVING max(forwarded_at) < $1 LIMIT $2
        """;

    private const string PendingSql = """
        SELECT m.id, m.forwarded_question, l.id, l.source_email, coalesce(l.data->>'title', l.file_name)
        FROM listing_chat_messages m JOIN listings l ON l.id = m.listing_id
        WHERE m.token_hash = $1 AND m.forward_status = 'pending' ORDER BY m.id FOR UPDATE OF m SKIP LOCKED
        """;

    private const string MarkSentSql = "UPDATE listing_chat_messages SET forward_status = 'sent', forward_batch = $2 WHERE id = ANY($1)";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Interval, timeProvider);
        do
        {
            try
            {
                await ForwardDueAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Forwarding chat questions failed, retrying in {Interval}", Interval);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ForwardDueAsync(CancellationToken ct)
    {
        DateTimeOffset bundledBefore = timeProvider.GetUtcNow() - TimeSpan.FromMinutes(options.Value.BundleMinutes);
        await using NpgsqlCommand command = dataSource.CreateCommand(DueSql);
        command.Parameters.Add(new NpgsqlParameter { Value = bundledBefore });
        command.Parameters.Add(new NpgsqlParameter { Value = MaxConversationsPerRun });
        List<string> conversations = [];
        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                conversations.Add(reader.GetString(0));
            }
        }

        foreach (string tokenHash in conversations)
        {
            await ForwardConversationAsync(tokenHash, ct);
        }
    }

    private async Task ForwardConversationAsync(string tokenHash, CancellationToken ct)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);
        ForwardedQuestionBatch? batch = await ReadBatchAsync(connection, transaction, tokenHash, ct);
        if (batch is null)
        {
            await transaction.CommitAsync(ct);
            return;
        }

        await emailSender.SendAsync(AgentQuestionMail.Compose(batch, options.Value.ReplyToAddress), ct);
        await using NpgsqlCommand update = new(MarkSentSql, connection, transaction);
        update.Parameters.Add(new NpgsqlParameter { Value = batch.MessageIds.ToArray() });
        update.Parameters.Add(new NpgsqlParameter { Value = batch.BatchId });
        await update.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
        logger.LogInformation("Forwarded {QuestionCount} questions for listing {ListingId} as {Reference}", batch.Questions.Count, batch.ListingId, batch.Reference);
    }

    private static async Task<ForwardedQuestionBatch?> ReadBatchAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string tokenHash,
        CancellationToken ct)
    {
        await using NpgsqlCommand command = new(PendingSql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter { Value = tokenHash });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        List<long> ids = [];
        List<string> questions = [];
        (Guid ListingId, string Agent, string Title)? listing = null;
        while (await reader.ReadAsync(ct))
        {
            ids.Add(reader.GetInt64(0));
            questions.Add(reader.GetString(1));
            listing ??= (reader.GetGuid(2), reader.GetString(3), await reader.IsDBNullAsync(4, ct) ? FallbackTitle : reader.GetString(4));
        }

        return listing is { } found
            ? new ForwardedQuestionBatch(Guid.NewGuid(), found.ListingId, found.Agent, found.Title, ids, questions)
            : null;
    }
}
