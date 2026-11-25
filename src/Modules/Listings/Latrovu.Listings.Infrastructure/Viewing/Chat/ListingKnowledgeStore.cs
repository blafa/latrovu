using Latrovu.Listings.Viewing.Chat;
using Npgsql;

namespace Latrovu.Listings.Infrastructure.Viewing.Chat;

public sealed class ListingKnowledgeStore(NpgsqlDataSource dataSource) : IListingKnowledgeStore
{
    private const string SelectSql = "SELECT id, question, answer, created_at FROM listing_knowledge WHERE listing_id = $1 ORDER BY created_at";

    private const string DeleteSql = "DELETE FROM listing_knowledge WHERE listing_id = $1 AND id = $2";

    public async Task<IReadOnlyList<KnowledgeEntry>> GetAsync(Guid listingId, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(SelectSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        List<KnowledgeEntry> entries = [];
        while (await reader.ReadAsync(ct))
        {
            entries.Add(new KnowledgeEntry(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetFieldValue<DateTimeOffset>(3)));
        }

        return entries;
    }

    public async Task<bool> RemoveAsync(Guid listingId, long entryId, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(DeleteSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = entryId });
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }
}
