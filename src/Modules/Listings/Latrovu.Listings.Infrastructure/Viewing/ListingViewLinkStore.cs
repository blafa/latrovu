using Npgsql;

namespace Latrovu.Listings.Infrastructure.Viewing;

public sealed class ListingViewLinkStore(NpgsqlDataSource dataSource, TimeProvider timeProvider) : IListingViewLinkStore
{
    private const string InsertSql = """
        INSERT INTO listing_view_links (token_hash, listing_id, search_id, created_at, expires_at)
        VALUES ($1, $2, $3, $4, $5)
        """;

    private const string FindSql = "SELECT listing_id FROM listing_view_links WHERE token_hash = $1 AND expires_at > $2";

    private const string RegisterViewSql = """
        UPDATE listing_view_links
        SET view_count = view_count + 1, first_viewed_at = COALESCE(first_viewed_at, $2)
        WHERE token_hash = $1
        """;

    public async Task<string> CreateAsync(Guid listingId, Guid? searchId, DateTimeOffset expiresAt, CancellationToken ct)
    {
        string token = AccessTokens.Create();
        await using NpgsqlCommand command = dataSource.CreateCommand(InsertSql);
        command.Parameters.Add(new NpgsqlParameter { Value = AccessTokens.Hash(token) });
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = (object?)searchId ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter { Value = timeProvider.GetUtcNow() });
        command.Parameters.Add(new NpgsqlParameter { Value = expiresAt });
        await command.ExecuteNonQueryAsync(ct);
        return token;
    }

    public async Task<Guid?> FindListingIdAsync(string token, DateTimeOffset now, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        await using NpgsqlCommand command = dataSource.CreateCommand(FindSql);
        command.Parameters.Add(new NpgsqlParameter { Value = AccessTokens.Hash(token) });
        command.Parameters.Add(new NpgsqlParameter { Value = now });
        return await command.ExecuteScalarAsync(ct) is Guid listingId ? listingId : null;
    }

    public async Task RegisterViewAsync(string token, DateTimeOffset now, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(RegisterViewSql);
        command.Parameters.Add(new NpgsqlParameter { Value = AccessTokens.Hash(token) });
        command.Parameters.Add(new NpgsqlParameter { Value = now });
        await command.ExecuteNonQueryAsync(ct);
    }
}
