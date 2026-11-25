using Npgsql;

namespace Latrovu.Listings.Infrastructure.Ingestion;

public sealed class ListingSourceMailStore(NpgsqlDataSource dataSource) : IListingSourceMailStore
{
    private const string InsertSql = """
        INSERT INTO listing_source_mails (listing_id, message_id)
        SELECT $1, $2 WHERE EXISTS (SELECT 1 FROM listings WHERE id = $1)
        ON CONFLICT DO NOTHING
        """;

    public async Task RecordAsync(Guid listingId, string messageId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        await using NpgsqlCommand command = dataSource.CreateCommand(InsertSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = messageId.Trim('<', '>', ' ') });
        await command.ExecuteNonQueryAsync(ct);
    }
}
