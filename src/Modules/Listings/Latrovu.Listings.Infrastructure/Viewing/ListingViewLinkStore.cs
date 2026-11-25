using Npgsql;

namespace Latrovu.Listings.Infrastructure.Viewing;

public sealed class ListingViewLinkStore(NpgsqlDataSource dataSource, TimeProvider timeProvider) : IListingViewLinkStore
{
    private const string PersonalKind = "personal";
    private const string SharedKind = "shared";

    private const string InsertSql = """
        INSERT INTO listing_view_links (token_hash, listing_id, search_id, created_at, expires_at, seeker_address, kind)
        VALUES ($1, $2, $3, $4, $5, $6, $7)
        ON CONFLICT (token_hash) DO NOTHING
        """;

    private const string FindSql = "SELECT listing_id, kind, expires_at FROM listing_view_links WHERE token_hash = $1 AND expires_at > $2";

    private const string RegisterViewSql = """
        UPDATE listing_view_links
        SET view_count = view_count + 1, first_viewed_at = COALESCE(first_viewed_at, $2)
        WHERE token_hash = $1
        """;

    public async Task<string> CreateAsync(Guid listingId, Guid? searchId, DateTimeOffset expiresAt, CancellationToken ct)
    {
        string token = AccessTokens.Create();
        await InsertAsync(new ListingViewLinkRow(AccessTokens.Hash(token), listingId, searchId, expiresAt, null, PersonalKind), ct);
        return token;
    }

    public async Task<string> CreateSharedAsync(Guid listingId, DateTimeOffset expiresAt, CancellationToken ct)
    {
        string token = AccessTokens.Create();
        await InsertAsync(new ListingViewLinkRow(AccessTokens.Hash(token), listingId, null, expiresAt, null, SharedKind), ct);
        return token;
    }

    public Task CreateFromHashAsync(Guid listingId, Guid searchId, string tokenHash, DateTimeOffset expiresAt, string? seekerAddress, CancellationToken ct) =>
        InsertAsync(new ListingViewLinkRow(tokenHash, listingId, searchId, expiresAt, seekerAddress, PersonalKind), ct);

    public async Task<ListingViewLink?> FindAsync(string token, DateTimeOffset now, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        await using NpgsqlCommand command = dataSource.CreateCommand(FindSql);
        command.Parameters.Add(new NpgsqlParameter { Value = AccessTokens.Hash(token) });
        command.Parameters.Add(new NpgsqlParameter { Value = now });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? new ListingViewLink(reader.GetGuid(0), reader.GetString(1) == SharedKind, reader.GetFieldValue<DateTimeOffset>(2))
            : null;
    }

    public async Task RegisterViewAsync(string token, DateTimeOffset now, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(RegisterViewSql);
        command.Parameters.Add(new NpgsqlParameter { Value = AccessTokens.Hash(token) });
        command.Parameters.Add(new NpgsqlParameter { Value = now });
        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task InsertAsync(ListingViewLinkRow row, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(InsertSql);
        command.Parameters.Add(new NpgsqlParameter { Value = row.TokenHash });
        command.Parameters.Add(new NpgsqlParameter { Value = row.ListingId });
        command.Parameters.Add(new NpgsqlParameter { Value = (object?)row.SearchId ?? DBNull.Value, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Uuid });
        command.Parameters.Add(new NpgsqlParameter { Value = timeProvider.GetUtcNow() });
        command.Parameters.Add(new NpgsqlParameter { Value = row.ExpiresAt });
        command.Parameters.Add(new NpgsqlParameter { Value = (object?)row.SeekerAddress ?? DBNull.Value, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar });
        command.Parameters.Add(new NpgsqlParameter { Value = row.Kind });
        await command.ExecuteNonQueryAsync(ct);
    }
}
