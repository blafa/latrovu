using Latrovu.BuildingBlocks.Retention;
using Npgsql;

namespace Latrovu.Listings.Infrastructure;

public sealed class ListingsRetentionTask(NpgsqlDataSource dataSource) : IRetentionTask
{
    public static readonly TimeSpan ExpiredLinkGrace = TimeSpan.FromDays(7);
    public static readonly TimeSpan AccessRequestRetention = TimeSpan.FromHours(24);
    public static readonly TimeSpan ProcessedIdRetention = TimeSpan.FromDays(7);

    private const string ExpiredLinksSql = """
        WITH expired AS (DELETE FROM listing_view_links WHERE expires_at < $1 RETURNING token_hash),
             chats AS (DELETE FROM listing_chat_messages WHERE token_hash IN (SELECT token_hash FROM expired) RETURNING 1),
             bookings AS (DELETE FROM listing_viewing_bookings WHERE token_hash IN (SELECT token_hash FROM expired) RETURNING 1)
        SELECT (SELECT count(*) FROM expired) + (SELECT count(*) FROM chats) + (SELECT count(*) FROM bookings)
        """;

    private static readonly string[] ExpiredAccessSql =
    [
        "DELETE FROM listing_access_tokens WHERE expires_at < $1",
        "DELETE FROM listing_verification_codes WHERE expires_at < $1",
        "DELETE FROM agent_review_sessions WHERE expires_at < $1",
        "DELETE FROM listing_renewal_tokens WHERE expires_at < $1"
    ];

    private const string AccessRequestsSql = "DELETE FROM listing_access_requests WHERE created_at < $1";

    private const string ProcessedSubmissionsSql = "DELETE FROM processed_submissions WHERE processed_at < $1";

    private const string EmptyProjectsSql = """
        DELETE FROM listing_projects p
        WHERE p.created_at < $1 AND NOT EXISTS (SELECT 1 FROM listings l WHERE l.project_id = p.id)
        """;

    public static readonly TimeSpan EmptyProjectGrace = TimeSpan.FromDays(1);

    public string Name => "Listings";

    public async Task<int> RunAsync(DateTimeOffset now, CancellationToken ct)
    {
        int deleted = 0;
        foreach (string sql in ExpiredAccessSql)
        {
            deleted += await DeleteAsync(sql, now, ct);
        }

        deleted += await DeleteExpiredLinksAsync(now - ExpiredLinkGrace, ct);
        deleted += await DeleteAsync(AccessRequestsSql, now - AccessRequestRetention, ct);
        deleted += await DeleteAsync(ProcessedSubmissionsSql, now - ProcessedIdRetention, ct);
        deleted += await DeleteAsync(EmptyProjectsSql, now - EmptyProjectGrace, ct);
        return deleted;
    }

    private async Task<int> DeleteAsync(string sql, DateTimeOffset cutoff, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter { Value = cutoff });
        return await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<int> DeleteExpiredLinksAsync(DateTimeOffset cutoff, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(ExpiredLinksSql);
        command.Parameters.Add(new NpgsqlParameter { Value = cutoff });
        return Convert.ToInt32((long)(await command.ExecuteScalarAsync(ct))!);
    }
}
