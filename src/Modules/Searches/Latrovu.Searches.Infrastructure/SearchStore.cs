using System.Text.Json;
using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;
using Npgsql;

namespace Latrovu.Searches.Infrastructure;

public sealed class SearchStore(NpgsqlDataSource dataSource, IOutbox outbox) : ISearchStore
{
    private const decimal PriceTolerance = 1.05m;

    private const string MatchSql = """
        SELECT s.id, s.seeker_address, s.channel, s.name, s.language, s.criteria::text, s.status
        FROM searches s
        WHERE s.status = 'active' AND s.area IS NOT NULL
          AND ST_Covers(s.area, ST_SetSRID(ST_MakePoint($1, $2), 2056))
          AND (s.criteria->>'offerType' IS NULL OR $3::text IS NULL OR s.criteria->>'offerType' = $3)
          AND ($4::numeric IS NULL OR s.criteria->>'roomsMin' IS NULL OR $4 >= (s.criteria->>'roomsMin')::numeric)
          AND ($4::numeric IS NULL OR s.criteria->>'roomsMax' IS NULL OR $4 <= (s.criteria->>'roomsMax')::numeric)
          AND ($5::numeric IS NULL OR s.criteria->>'priceMaxChf' IS NULL OR $5 <= (s.criteria->>'priceMaxChf')::numeric * $8)
          AND ($6::numeric IS NULL OR s.criteria->>'livingAreaMinM2' IS NULL OR $6 >= (s.criteria->>'livingAreaMinM2')::numeric)
          AND NOT EXISTS (SELECT 1 FROM search_alerts a WHERE a.search_id = s.id AND a.listing_id = $7)
        """;

    private const string RecordAlertSql = "INSERT INTO search_alerts (search_id, listing_id) VALUES ($1, $2) ON CONFLICT DO NOTHING";
    private const string SelectSql = """
        SELECT id, seeker_address, channel, name, language, criteria::text, status
        FROM searches WHERE seeker_address = $1 ORDER BY created_at
        """;

    private const string UpsertSql = """
        INSERT INTO searches (id, seeker_address, channel, name, language, criteria, status)
        VALUES ($1, $2, $3, $4, $5, $6::jsonb, $7)
        ON CONFLICT (id) DO UPDATE SET name = $4, language = $5, criteria = $6::jsonb, status = $7, updated_at = now()
        """;

    private const string ProcessedSql = "SELECT EXISTS (SELECT 1 FROM processed_search_requests WHERE source_id = $1)";

    private const string SetAreaSql = """
        UPDATE searches SET place_refs = $2::jsonb, updated_at = now(), area = (
            SELECT ST_Multi(ST_CollectionExtract(ST_Union(
                       CASE WHEN refs.buffer_meters > 0 THEN ST_Buffer(p.geom, refs.buffer_meters) ELSE p.geom END), 3))
            FROM jsonb_to_recordset($2::jsonb) AS refs(place_id bigint, buffer_meters double precision)
            JOIN places p ON p.id = refs.place_id)
        WHERE id = $1
        """;

    private static readonly JsonSerializerOptions SnakeCase = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private const string MarkProcessedSql = "INSERT INTO processed_search_requests (source_id) VALUES ($1) ON CONFLICT DO NOTHING";

    public async Task<IReadOnlyList<Search>> FindBySeekerAsync(string seekerAddress, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(SelectSql);
        command.Parameters.Add(new NpgsqlParameter { Value = seekerAddress });
        return await ReadSearchesAsync(command, ct);
    }

    public async Task<IReadOnlyList<Search>> FindMatchesAsync(ListingReadyForMatching listing, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(listing);

        await using NpgsqlCommand command = dataSource.CreateCommand(MatchSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listing.East });
        command.Parameters.Add(new NpgsqlParameter { Value = listing.North });
        command.Parameters.Add(new NpgsqlParameter { Value = (object?)listing.OfferType ?? DBNull.Value, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { Value = (object?)listing.Rooms ?? DBNull.Value, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Numeric });
        command.Parameters.Add(new NpgsqlParameter { Value = (object?)listing.PriceChf ?? DBNull.Value, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Numeric });
        command.Parameters.Add(new NpgsqlParameter { Value = (object?)listing.LivingAreaM2 ?? DBNull.Value, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Numeric });
        command.Parameters.Add(new NpgsqlParameter { Value = listing.ListingId });
        command.Parameters.Add(new NpgsqlParameter { Value = PriceTolerance });
        return await ReadSearchesAsync(command, ct);
    }

    public async Task<bool> RecordAlertAsync(ViewLinkRequested linkRequest, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(linkRequest);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(ct);
        await using NpgsqlCommand command = new(RecordAlertSql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter { Value = linkRequest.SearchId });
        command.Parameters.Add(new NpgsqlParameter { Value = linkRequest.ListingId });
        bool recorded = await command.ExecuteNonQueryAsync(ct) > 0;
        if (recorded)
        {
            await outbox.AddAsync([linkRequest], transaction, ct);
        }

        await transaction.CommitAsync(ct);
        return recorded;
    }

    private static async Task<IReadOnlyList<Search>> ReadSearchesAsync(NpgsqlCommand command, CancellationToken ct)
    {
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        List<Search> searches = [];
        while (await reader.ReadAsync(ct))
        {
            searches.Add(new Search(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6)));
        }

        return searches;
    }

    public async Task SaveAsync(Search search, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(search);

        await using NpgsqlCommand command = dataSource.CreateCommand(UpsertSql);
        foreach (object value in new object[] { search.Id, search.SeekerAddress, search.Channel, search.Name, search.Language, search.CriteriaJson, search.Status })
        {
            command.Parameters.Add(new NpgsqlParameter { Value = value });
        }

        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task SetAreaAsync(Guid searchId, IReadOnlyList<PlaceRef> places, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(places);

        await using NpgsqlCommand command = dataSource.CreateCommand(SetAreaSql);
        command.Parameters.Add(new NpgsqlParameter { Value = searchId });
        command.Parameters.Add(new NpgsqlParameter { Value = JsonSerializer.Serialize(places, SnakeCase) });
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> IsProcessedAsync(string sourceId, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(ProcessedSql);
        command.Parameters.Add(new NpgsqlParameter { Value = sourceId });
        return await command.ExecuteScalarAsync(ct) is true;
    }

    public async Task MarkProcessedAsync(string sourceId, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(MarkProcessedSql);
        command.Parameters.Add(new NpgsqlParameter { Value = sourceId });
        await command.ExecuteNonQueryAsync(ct);
    }
}
