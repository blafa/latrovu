using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;
using Latrovu.Listings.Viewing.Access;
using Npgsql;

namespace Latrovu.Listings.Infrastructure.Viewing.Access;

public sealed class ListingAccessRequestStore(NpgsqlDataSource dataSource, IOutbox outbox) : IListingAccessRequestStore
{
    private const string CountSql = """
        SELECT (SELECT count(*) FROM listing_access_requests WHERE token_hash = $1),
               (SELECT count(*) FROM listing_access_requests WHERE lower(address) = lower($2) AND created_at > $3)
        """;

    private const string InsertSql = "INSERT INTO listing_access_requests (token_hash, listing_id, address, created_at) VALUES ($1, $2, $3, $4)";

    public async Task<AccessRequestCounts> CountAsync(string tokenHash, string address, DateTimeOffset addressWindowStart, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(CountSql);
        command.Parameters.Add(new NpgsqlParameter { Value = tokenHash });
        command.Parameters.Add(new NpgsqlParameter { Value = address });
        command.Parameters.Add(new NpgsqlParameter { Value = addressWindowStart });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return new AccessRequestCounts((int)reader.GetInt64(0), (int)reader.GetInt64(1));
    }

    public async Task RecordAsync(string tokenHash, ListingAccessRequested request, DateTimeOffset now, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);
        await using NpgsqlCommand command = new(InsertSql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter { Value = tokenHash });
        command.Parameters.Add(new NpgsqlParameter { Value = request.ListingId });
        command.Parameters.Add(new NpgsqlParameter { Value = request.Address });
        command.Parameters.Add(new NpgsqlParameter { Value = now });
        await command.ExecuteNonQueryAsync(ct);
        await outbox.AddAsync([request], transaction, ct);
        await transaction.CommitAsync(ct);
    }
}
