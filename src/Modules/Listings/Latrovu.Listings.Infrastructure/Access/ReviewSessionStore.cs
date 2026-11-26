using Npgsql;

namespace Latrovu.Listings.Infrastructure.Access;

public sealed class ReviewSessionStore(NpgsqlDataSource dataSource) : IReviewSessionStore
{
    private const string InsertSql =
        "INSERT INTO agent_review_sessions (session_hash, source_email, expires_at) VALUES ($1, $2, $3)";

    private const string FindSql =
        "SELECT source_email FROM agent_review_sessions WHERE session_hash = $1 AND expires_at > $2";

    private const string DeleteSql = "DELETE FROM agent_review_sessions WHERE session_hash = $1";

    public async Task CreateAsync(string sourceEmail, string sessionHash, DateTimeOffset expiresAt, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(InsertSql);
        command.Parameters.Add(new NpgsqlParameter { Value = sessionHash });
        command.Parameters.Add(new NpgsqlParameter { Value = sourceEmail });
        command.Parameters.Add(new NpgsqlParameter { Value = expiresAt });
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<string?> FindSourceEmailAsync(string sessionHash, DateTimeOffset now, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(FindSql);
        command.Parameters.Add(new NpgsqlParameter { Value = sessionHash });
        command.Parameters.Add(new NpgsqlParameter { Value = now });
        return await command.ExecuteScalarAsync(ct) as string;
    }

    public async Task DeleteAsync(string sessionHash, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(DeleteSql);
        command.Parameters.Add(new NpgsqlParameter { Value = sessionHash });
        await command.ExecuteNonQueryAsync(ct);
    }
}
