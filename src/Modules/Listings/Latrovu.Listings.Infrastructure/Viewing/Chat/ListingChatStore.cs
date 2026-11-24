using Latrovu.Listings.Viewing.Chat;
using Npgsql;
using NpgsqlTypes;

namespace Latrovu.Listings.Infrastructure.Viewing.Chat;

public sealed class ListingChatStore(NpgsqlDataSource dataSource) : IListingChatStore
{
    private const string SelectSql = """
        SELECT role, text, source, page, created_at, id, forward_status FROM listing_chat_messages
        WHERE token_hash = $1 ORDER BY created_at, id
        """;

    private const string InsertSql = """
        INSERT INTO listing_chat_messages (token_hash, listing_id, role, text, source, page, created_at)
        VALUES ($1, $2, $3, $4, $5, $6, $7)
        """;

    private const string CountSql = """
        SELECT count(*) FROM listing_chat_messages WHERE token_hash = $1 AND role = $2 AND created_at > $3
        """;

    private const string MarkForwardSql = """
        UPDATE listing_chat_messages SET forward_status = 'pending', forwarded_question = $3, forwarded_at = $4
        WHERE token_hash = $1 AND id = $2 AND forward_status IS NULL
        """;

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string token, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(SelectSql);
        command.Parameters.Add(new NpgsqlParameter { Value = AccessTokens.Hash(token) });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        List<ChatMessage> messages = [];
        while (await reader.ReadAsync(ct))
        {
            messages.Add(new ChatMessage(
                reader.GetString(0),
                reader.GetString(1),
                await reader.IsDBNullAsync(2, ct) ? null : reader.GetString(2),
                await reader.IsDBNullAsync(3, ct) ? null : reader.GetInt32(3),
                reader.GetFieldValue<DateTimeOffset>(4),
                reader.GetInt64(5),
                await reader.IsDBNullAsync(6, ct) ? null : reader.GetString(6)));
        }

        return messages;
    }

    public async Task AddAsync(string token, Guid listingId, ChatMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        await using NpgsqlCommand command = dataSource.CreateCommand(InsertSql);
        command.Parameters.Add(new NpgsqlParameter { Value = AccessTokens.Hash(token) });
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = message.Role });
        command.Parameters.Add(new NpgsqlParameter { Value = message.Text });
        command.Parameters.Add(new NpgsqlParameter { Value = (object?)message.Source ?? DBNull.Value, NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { Value = (object?)message.Page ?? DBNull.Value, NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { Value = message.CreatedAt });
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> MarkForwardAsync(string token, long answerId, string question, DateTimeOffset now, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(MarkForwardSql);
        command.Parameters.Add(new NpgsqlParameter { Value = AccessTokens.Hash(token) });
        command.Parameters.Add(new NpgsqlParameter { Value = answerId });
        command.Parameters.Add(new NpgsqlParameter { Value = question });
        command.Parameters.Add(new NpgsqlParameter { Value = now });
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<int> CountSeekerMessagesSinceAsync(string token, DateTimeOffset since, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(CountSql);
        command.Parameters.Add(new NpgsqlParameter { Value = AccessTokens.Hash(token) });
        command.Parameters.Add(new NpgsqlParameter { Value = ChatMessage.SeekerRole });
        command.Parameters.Add(new NpgsqlParameter { Value = since });
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture);
    }
}
