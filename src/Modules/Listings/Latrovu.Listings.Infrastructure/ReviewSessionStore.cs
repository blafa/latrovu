using Npgsql;

namespace Latrovu.Listings.Infrastructure;

public sealed class ReviewSessionStore(NpgsqlDataSource dataSource) : IReviewSessionStore
{
    private const string InsertSql =
        "INSERT INTO listing_review_sessions (session_hash, listing_id, expires_at) VALUES ($1, $2, $3)";

    private const string FindSql =
        "SELECT listing_id FROM listing_review_sessions WHERE session_hash = $1 AND expires_at > $2";

    public async Task CreateAsync(Guid listingId, string sessionHash, DateTimeOffset expiresAt, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(InsertSql);
        command.Parameters.Add(new NpgsqlParameter { Value = sessionHash });
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = expiresAt });
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<Guid?> FindListingIdAsync(string sessionHash, DateTimeOffset now, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(FindSql);
        command.Parameters.Add(new NpgsqlParameter { Value = sessionHash });
        command.Parameters.Add(new NpgsqlParameter { Value = now });
        object? result = await command.ExecuteScalarAsync(ct);
        return result is Guid listingId ? listingId : null;
    }
}
