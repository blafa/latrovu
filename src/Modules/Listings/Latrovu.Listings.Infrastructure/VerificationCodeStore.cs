using Npgsql;

namespace Latrovu.Listings.Infrastructure;

public sealed class VerificationCodeStore(NpgsqlDataSource dataSource, TimeProvider timeProvider) : IVerificationCodeStore
{
    private const string CountSql =
        "SELECT count(*) FROM listing_verification_codes WHERE listing_id = $1 AND created_at >= $2";

    private const string InsertSql = """
        INSERT INTO listing_verification_codes (id, listing_id, code_hash, created_at, expires_at)
        VALUES ($1, $2, $3, $4, $5)
        """;

    private const string FindLatestSql = """
        SELECT id, code_hash, attempts FROM listing_verification_codes
        WHERE listing_id = $1 AND expires_at > $2 ORDER BY created_at DESC LIMIT 1
        """;

    private const string IncrementSql = "UPDATE listing_verification_codes SET attempts = attempts + 1 WHERE id = $1";

    private const string DeleteSql = "DELETE FROM listing_verification_codes WHERE listing_id = $1";

    public async Task<int> CountCreatedSinceAsync(Guid listingId, DateTimeOffset since, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(CountSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = since });
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }

    public async Task SaveAsync(Guid listingId, string codeHash, DateTimeOffset expiresAt, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(InsertSql);
        command.Parameters.Add(new NpgsqlParameter { Value = Guid.NewGuid() });
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = codeHash });
        command.Parameters.Add(new NpgsqlParameter { Value = timeProvider.GetUtcNow() });
        command.Parameters.Add(new NpgsqlParameter { Value = expiresAt });
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<ActiveVerificationCode?> FindLatestActiveAsync(Guid listingId, DateTimeOffset now, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(FindLatestSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = now });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? new ActiveVerificationCode(reader.GetGuid(0), reader.GetString(1), reader.GetInt32(2))
            : null;
    }

    public async Task IncrementAttemptsAsync(Guid codeId, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(IncrementSql);
        command.Parameters.Add(new NpgsqlParameter { Value = codeId });
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAllAsync(Guid listingId, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(DeleteSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        await command.ExecuteNonQueryAsync(ct);
    }
}
