using System.Text.Json;
using Npgsql;

namespace Latrovu.Searches.Infrastructure;

public sealed class SearchStore(NpgsqlDataSource dataSource) : ISearchStore
{
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
