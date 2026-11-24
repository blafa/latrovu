using Latrovu.BuildingBlocks.Geo;
using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;
using Latrovu.Listings.Matching;
using Npgsql;

namespace Latrovu.Listings.Infrastructure.Matching;

public sealed class ListingLocationStore(NpgsqlDataSource dataSource, IOutbox outbox) : IListingLocationStore
{
    private const string UpdateSql = """
        UPDATE listings SET location = ST_SetSRID(ST_MakePoint($2, $3), 2056), location_precision = $4 WHERE id = $1
        """;

    public async Task SaveAsync(Guid listingId, GeoPoint point, ListingReadyForMatching announcement, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(point);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);
        await using NpgsqlCommand command = new(UpdateSql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = point.East });
        command.Parameters.Add(new NpgsqlParameter { Value = point.North });
        command.Parameters.Add(new NpgsqlParameter { Value = point.Precision });
        await command.ExecuteNonQueryAsync(ct);
        await outbox.AddAsync([announcement], transaction, ct);
        await transaction.CommitAsync(ct);
    }
}
