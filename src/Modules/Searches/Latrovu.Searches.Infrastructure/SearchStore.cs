using System.Text.Json;
using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;
using Npgsql;
using NpgsqlTypes;

namespace Latrovu.Searches.Infrastructure;

public sealed class SearchStore(NpgsqlDataSource dataSource, IOutbox outbox) : ISearchStore
{
    private const string SearchColumns = "s.id, s.seeker_address, s.channel, s.name, s.language, s.criteria::text, s.status";

    private const string SelectSql = $"SELECT {SearchColumns} FROM searches s WHERE s.seeker_address = $1 ORDER BY s.created_at";

    private static readonly string MatchSql = $"""
        SELECT {SearchColumns}
        FROM searches s
        WHERE s.status = 'active' AND s.area IS NOT NULL
          AND ST_Covers(s.area, ST_SetSRID(ST_MakePoint($1, $2), 2056))
          AND {SearchCriteria.Condition("$3::text", "$4::numeric", "$5::numeric", "$6::numeric")}
          AND NOT EXISTS (SELECT 1 FROM search_alerts a WHERE a.search_id = s.id AND a.listing_id = $7)
        """;

    private static readonly string CandidatesSql = $"""
        SELECT m.listing_id, ST_X(m.location), ST_Y(m.location), m.location_precision, m.offer_type, m.rooms, m.price_chf,
               m.living_area_m2, m.available_from, m.title, m.city
        FROM matchable_listings m
        JOIN searches s ON s.id = $1
        WHERE s.area IS NOT NULL AND ST_Covers(s.area, m.location)
          AND {SearchCriteria.Condition("m.offer_type", "m.rooms", "m.price_chf", "m.living_area_m2")}
          AND NOT EXISTS (SELECT 1 FROM search_alerts a WHERE a.search_id = s.id AND a.listing_id = m.listing_id)
        ORDER BY m.updated_at DESC
        LIMIT $2
        """;

    private const string SaveCandidateSql = """
        INSERT INTO matchable_listings (listing_id, location, location_precision, offer_type, rooms, price_chf, living_area_m2, available_from, title, city)
        VALUES ($1, ST_SetSRID(ST_MakePoint($2, $3), 2056), $4, $5, $6, $7, $8, $9, $10, $11)
        ON CONFLICT (listing_id) DO UPDATE SET location = EXCLUDED.location, location_precision = EXCLUDED.location_precision,
            offer_type = EXCLUDED.offer_type, rooms = EXCLUDED.rooms, price_chf = EXCLUDED.price_chf,
            living_area_m2 = EXCLUDED.living_area_m2, available_from = EXCLUDED.available_from, title = EXCLUDED.title,
            city = EXCLUDED.city, updated_at = now()
        """;

    private const string RemoveCandidateSql = "DELETE FROM matchable_listings WHERE listing_id = $1";

    private const string UpsertSql = """
        INSERT INTO searches (id, seeker_address, channel, name, language, criteria, status)
        VALUES ($1, $2, $3, $4, $5, $6::jsonb, $7)
        ON CONFLICT (id) DO UPDATE SET name = $4, language = $5, criteria = $6::jsonb, status = $7, updated_at = now()
        """;

    private const string SetAreaSql = """
        UPDATE searches SET place_refs = $2::jsonb, updated_at = now(), area = (
            SELECT ST_Multi(ST_CollectionExtract(ST_Union(
                       CASE WHEN refs.buffer_meters > 0 THEN ST_Buffer(p.geom, refs.buffer_meters) ELSE p.geom END), 3))
            FROM jsonb_to_recordset($2::jsonb) AS refs(place_id bigint, buffer_meters double precision)
            JOIN places p ON p.id = refs.place_id)
        WHERE id = $1
        """;

    private const string RecordAlertSql = "INSERT INTO search_alerts (search_id, listing_id) VALUES ($1, $2) ON CONFLICT DO NOTHING";

    private const string ProcessedSql = "SELECT EXISTS (SELECT 1 FROM processed_search_requests WHERE source_id = $1)";

    private const string MarkProcessedSql = "INSERT INTO processed_search_requests (source_id) VALUES ($1) ON CONFLICT DO NOTHING";

    private static readonly JsonSerializerOptions SnakeCase = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public async Task<IReadOnlyList<Search>> FindBySeekerAsync(string seekerAddress, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(SelectSql);
        command.Parameters.Add(new NpgsqlParameter { Value = seekerAddress });
        return await ReadSearchesAsync(command, ct);
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

    public async Task<IReadOnlyList<Search>> FindMatchesAsync(ListingReadyForMatching listing, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(listing);

        await using NpgsqlCommand command = dataSource.CreateCommand(MatchSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listing.East });
        command.Parameters.Add(new NpgsqlParameter { Value = listing.North });
        command.Parameters.Add(Nullable(listing.OfferType, NpgsqlDbType.Text));
        command.Parameters.Add(Nullable(listing.Rooms, NpgsqlDbType.Numeric));
        command.Parameters.Add(Nullable(listing.PriceChf, NpgsqlDbType.Numeric));
        command.Parameters.Add(Nullable(listing.LivingAreaM2, NpgsqlDbType.Numeric));
        command.Parameters.Add(new NpgsqlParameter { Value = listing.ListingId });
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

    public async Task SaveCandidateAsync(ListingReadyForMatching listing, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(listing);

        await using NpgsqlCommand command = dataSource.CreateCommand(SaveCandidateSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listing.ListingId });
        command.Parameters.Add(new NpgsqlParameter { Value = listing.East });
        command.Parameters.Add(new NpgsqlParameter { Value = listing.North });
        command.Parameters.Add(new NpgsqlParameter { Value = listing.LocationPrecision });
        command.Parameters.Add(Nullable(listing.OfferType, NpgsqlDbType.Text));
        command.Parameters.Add(Nullable(listing.Rooms, NpgsqlDbType.Numeric));
        command.Parameters.Add(Nullable(listing.PriceChf, NpgsqlDbType.Numeric));
        command.Parameters.Add(Nullable(listing.LivingAreaM2, NpgsqlDbType.Numeric));
        command.Parameters.Add(Nullable(listing.AvailableFrom, NpgsqlDbType.Text));
        command.Parameters.Add(Nullable(listing.Title, NpgsqlDbType.Text));
        command.Parameters.Add(Nullable(listing.City, NpgsqlDbType.Text));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task RemoveCandidateAsync(Guid listingId, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(RemoveCandidateSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<ListingReadyForMatching>> FindCandidatesForSearchAsync(Guid searchId, int limit, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(CandidatesSql);
        command.Parameters.Add(new NpgsqlParameter { Value = searchId });
        command.Parameters.Add(new NpgsqlParameter { Value = limit });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        List<ListingReadyForMatching> candidates = [];
        while (await reader.ReadAsync(ct))
        {
            candidates.Add(await ReadCandidateAsync(reader, ct));
        }

        return candidates;
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

    private static async Task<ListingReadyForMatching> ReadCandidateAsync(NpgsqlDataReader reader, CancellationToken ct) =>
        new(
            reader.GetGuid(0),
            reader.GetDouble(1),
            reader.GetDouble(2),
            reader.GetString(3),
            await reader.IsDBNullAsync(4, ct) ? null : reader.GetString(4),
            await reader.IsDBNullAsync(5, ct) ? null : reader.GetDecimal(5),
            await reader.IsDBNullAsync(6, ct) ? null : reader.GetDecimal(6),
            await reader.IsDBNullAsync(7, ct) ? null : reader.GetDecimal(7),
            await reader.IsDBNullAsync(8, ct) ? null : reader.GetString(8),
            await reader.IsDBNullAsync(9, ct) ? null : reader.GetString(9),
            await reader.IsDBNullAsync(10, ct) ? null : reader.GetString(10));

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

    private static NpgsqlParameter Nullable(object? value, NpgsqlDbType type) =>
        new() { Value = value ?? DBNull.Value, NpgsqlDbType = type };
}
