using Npgsql;

namespace Latrovu.Listings.Infrastructure;

public sealed class ListingAccessTokenStore(NpgsqlDataSource dataSource, TimeProvider timeProvider) : IListingAccessTokenStore
{
    public const int ValidityDays = 30;

    private const string InsertSql =
        "INSERT INTO listing_access_tokens (token_hash, listing_id, expires_at) VALUES ($1, $2, $3)";

    private const string FindSql =
        "SELECT listing_id FROM listing_access_tokens WHERE token_hash = $1 AND expires_at > $2";

    public async Task<string> CreateAsync(Guid listingId, CancellationToken ct)
    {
        string token = AccessTokens.Create();
        await using NpgsqlCommand command = dataSource.CreateCommand(InsertSql);
        command.Parameters.Add(new NpgsqlParameter { Value = AccessTokens.Hash(token) });
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = timeProvider.GetUtcNow().AddDays(ValidityDays) });
        await command.ExecuteNonQueryAsync(ct);
        return token;
    }

    public async Task<Guid?> FindListingIdAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        await using NpgsqlCommand command = dataSource.CreateCommand(FindSql);
        command.Parameters.Add(new NpgsqlParameter { Value = AccessTokens.Hash(token) });
        command.Parameters.Add(new NpgsqlParameter { Value = timeProvider.GetUtcNow() });
        object? result = await command.ExecuteScalarAsync(ct);
        return result is Guid listingId ? listingId : null;
    }
}
