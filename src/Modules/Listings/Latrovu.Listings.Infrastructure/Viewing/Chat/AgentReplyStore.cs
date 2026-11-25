using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;
using Latrovu.Listings.Viewing.Chat;
using Npgsql;

namespace Latrovu.Listings.Infrastructure.Viewing.Chat;

public sealed class AgentReplyStore(NpgsqlDataSource dataSource, IOutbox outbox) : IAgentReplyStore
{
    private const string ReferencePrefix = "Q-";
    private const int ReferenceHexLength = 10;
    private const string ProcessedPrefix = "agent-reply:";
    private const string FollowUpConfirmation = "Ihre Nachricht haben wir an den Anbieter weitergeleitet.";

    private const string FindSql = """
        SELECT m.forward_batch, m.listing_id, m.token_hash, l.source_email, coalesce(l.data->>'title', l.file_name, ''),
               v.search_id, m.forwarded_question, coalesce(l.data::text, '{}'), v.seeker_address
        FROM listing_chat_messages m
        JOIN listings l ON l.id = m.listing_id
        LEFT JOIN listing_view_links v ON v.token_hash = m.token_hash
        WHERE m.forward_batch IS NOT NULL AND replace(m.forward_batch::text, '-', '') LIKE $1
        ORDER BY m.id
        """;

    private const string ProcessedSql = "INSERT INTO processed_submissions (source_id) VALUES ($1) ON CONFLICT DO NOTHING";

    private const string InsertAnswerSql = """
        INSERT INTO listing_chat_messages (token_hash, listing_id, role, text, source, original_text, created_at)
        VALUES ($1, $2, $3, $4, $5, $6, $7)
        """;

    private const string InsertSeekerSql = """
        INSERT INTO listing_chat_messages (token_hash, listing_id, role, text, created_at) VALUES ($1, $2, $3, $4, $5)
        """;

    private const string InsertForwardSql = """
        INSERT INTO listing_chat_messages (token_hash, listing_id, role, text, source, forward_status, forwarded_question, forwarded_at, created_at)
        VALUES ($1, $2, $3, $4, $5, 'pending', $6, $7, $7)
        """;

    private const string MarkAnsweredSql = "UPDATE listing_chat_messages SET forward_status = 'answered' WHERE forward_batch = $1";

    private const string InsertKnowledgeSql = "INSERT INTO listing_knowledge (listing_id, question, answer, created_at) VALUES ($1, $2, $3, $4)";

    public async Task<AgentReplyTarget?> FindAsync(string reference, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(reference);

        await using NpgsqlCommand command = dataSource.CreateCommand(FindSql);
        command.Parameters.Add(new NpgsqlParameter { Value = reference.Replace(ReferencePrefix, string.Empty, StringComparison.OrdinalIgnoreCase) + "%" });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        AgentReplyTarget? target = null;
        List<string> questions = [];
        while (await reader.ReadAsync(ct))
        {
            target ??= await ReadTargetAsync(reader, questions, ct);
            questions.Add(reader.GetString(6));
        }

        return target;
    }

    public async Task<bool> RecordAsync(
        AgentReplyTarget target,
        TranslatedAnswer answer,
        string originalAnswer,
        string sourceMessageId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(answer);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);
        if (!await MarkProcessedAsync(connection, transaction, sourceMessageId, ct))
        {
            return false;
        }

        object source = answer.Translated ? ChatMessage.TranslatedSource : DBNull.Value;
        await ExecuteAsync(connection, transaction, InsertAnswerSql, [target.TokenHash, target.ListingId, ChatMessage.AgentRole, answer.Text, source, originalAnswer, now], ct);
        await ExecuteAsync(connection, transaction, MarkAnsweredSql, [target.BatchId], ct);
        if (answer.General)
        {
            await ExecuteAsync(connection, transaction, InsertKnowledgeSql, [target.ListingId, string.Join("\n", target.Questions), answer.Text, now], ct);
        }

        if (target.SearchId is { } searchId)
        {
            AgentAnswered answered = new(searchId, target.ListingId, target.Title, target.Questions, answer.Text, Reference(target));
            await outbox.AddAsync([answered], transaction, ct);
        }

        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<bool> RecordSeekerFollowUpAsync(
        AgentReplyTarget target,
        string text,
        string sourceMessageId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(target);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);
        if (!await MarkProcessedAsync(connection, transaction, sourceMessageId, ct))
        {
            return false;
        }

        await ExecuteAsync(connection, transaction, InsertSeekerSql, [target.TokenHash, target.ListingId, ChatMessage.SeekerRole, text, now], ct);
        await ExecuteAsync(
            connection,
            transaction,
            InsertForwardSql,
            [target.TokenHash, target.ListingId, ChatMessage.AssistantRole, FollowUpConfirmation, ChatMessage.RequestSource, text, now.AddTicks(1)],
            ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private static string Reference(AgentReplyTarget target) => ReferencePrefix + target.BatchId.ToString("N")[..ReferenceHexLength];

    private static async Task<AgentReplyTarget> ReadTargetAsync(NpgsqlDataReader reader, List<string> questions, CancellationToken ct) =>
        new(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            await reader.IsDBNullAsync(5, ct) ? null : reader.GetGuid(5),
            questions,
            reader.GetString(7),
            await reader.IsDBNullAsync(8, ct) ? null : reader.GetString(8));

    private static async Task<bool> MarkProcessedAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string sourceMessageId, CancellationToken ct)
    {
        if (await ExecuteAsync(connection, transaction, ProcessedSql, [ProcessedPrefix + sourceMessageId], ct) > 0)
        {
            return true;
        }

        await transaction.RollbackAsync(ct);
        return false;
    }

    private static async Task<int> ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        object[] values,
        CancellationToken ct)
    {
        await using NpgsqlCommand command = new(sql, connection, transaction);
        foreach (object value in values)
        {
            command.Parameters.Add(new NpgsqlParameter { Value = value });
        }

        return await command.ExecuteNonQueryAsync(ct);
    }
}
