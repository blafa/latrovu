using System.Text.Json;
using Latrovu.Listings.Viewing.Chat;
using Npgsql;

namespace Latrovu.Listings.Infrastructure.Viewing.Chat;

public sealed class ListingSourceTextStore(NpgsqlDataSource dataSource) : IListingSourceTextStore
{
    private const string SelectSql = "SELECT source_pages::text FROM listings WHERE id = $1 AND source_pages IS NOT NULL";

    public async Task<IReadOnlyList<string>> GetPagesAsync(Guid listingId, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(SelectSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        return await command.ExecuteScalarAsync(ct) is string json
            ? JsonSerializer.Deserialize<List<string>>(json) ?? []
            : [];
    }
}
