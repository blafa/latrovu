using Latrovu.Listings.Renewal;
using Npgsql;

namespace Latrovu.Listings.Infrastructure.Renewal;

public sealed class ListingRenewalStore(NpgsqlDataSource dataSource) : IListingRenewalStore
{
    private const string ApprovedSql = """
        SELECT id, source_email, coalesce(data->>'title', file_name, 'Ihr Inserat'), coalesce(renewed_at, created_at), reminders_sent, last_reminder_at
        FROM listings WHERE status = 'approved'
        """;

    private const string MarkReminderSql = "UPDATE listings SET reminders_sent = $2, last_reminder_at = $3 WHERE id = $1";

    private const string InsertTokenSql = "INSERT INTO listing_renewal_tokens (token_hash, listing_id, expires_at) VALUES ($1, $2, $3)";

    private const string FindTokenSql = "SELECT listing_id FROM listing_renewal_tokens WHERE token_hash = $1 AND expires_at > $2";

    private const string RenewSql = """
        UPDATE listings SET renewed_at = $2, reminders_sent = 0, last_reminder_at = NULL
        WHERE id = $1 AND status = 'approved'
        """;

    public async Task<IReadOnlyList<RenewalState>> GetApprovedAsync(CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(ApprovedSql);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        List<RenewalState> states = [];
        while (await reader.ReadAsync(ct))
        {
            states.Add(new RenewalState(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetFieldValue<DateTimeOffset>(3),
                reader.GetInt32(4),
                await reader.IsDBNullAsync(5, ct) ? null : reader.GetFieldValue<DateTimeOffset>(5)));
        }

        return states;
    }

    public async Task MarkReminderSentAsync(Guid listingId, int number, DateTimeOffset now, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(MarkReminderSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = number });
        command.Parameters.Add(new NpgsqlParameter { Value = now });
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<string> CreateTokenAsync(Guid listingId, DateTimeOffset expiresAt, CancellationToken ct)
    {
        string token = AccessTokens.Create();
        await using NpgsqlCommand command = dataSource.CreateCommand(InsertTokenSql);
        command.Parameters.Add(new NpgsqlParameter { Value = AccessTokens.Hash(token) });
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = expiresAt });
        await command.ExecuteNonQueryAsync(ct);
        return token;
    }

    public async Task<Guid?> FindListingByTokenAsync(string token, DateTimeOffset now, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        await using NpgsqlCommand command = dataSource.CreateCommand(FindTokenSql);
        command.Parameters.Add(new NpgsqlParameter { Value = AccessTokens.Hash(token) });
        command.Parameters.Add(new NpgsqlParameter { Value = now });
        return await command.ExecuteScalarAsync(ct) is Guid listingId ? listingId : null;
    }

    public async Task<bool> RenewAsync(Guid listingId, DateTimeOffset now, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(RenewSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = now });
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }
}
