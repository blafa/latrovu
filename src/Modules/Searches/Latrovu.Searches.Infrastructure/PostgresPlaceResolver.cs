using Npgsql;

namespace Latrovu.Searches.Infrastructure;

public sealed class PostgresPlaceResolver(NpgsqlDataSource dataSource, ILogger<PostgresPlaceResolver> logger) : IPlaceResolver
{
    private const double MinimumFuzzyScore = 0.5;
    private const double MetersPerKilometer = 1000;
    private const double DefaultLakeShoreMeters = 1000;
    private const string LakeKind = "lake";
    private const char ContextSeparator = ',';

    private static readonly string[] KindPriority =
        ["municipality", "locality", "district", "alias", "quarter", "settlement", "region", "canton", "lake", "quarter_part", "zip"];

    private static readonly string[] SupportedKindHints = ["canton", "region", LakeKind];

    private const string LookupSql = """
        WITH within_area AS (
            SELECT ST_Union(geom) AS geom FROM places
            WHERE $2::text IS NOT NULL AND search_name = lower(unaccent($2))
              AND kind IN ('canton', 'region', 'municipality', 'locality', 'settlement'))
        SELECT p.id, p.kind, p.name, p.context, p.search_name = lower(unaccent($1)) AS exact,
               similarity(p.search_name, lower(unaccent($1))) AS score
        FROM places p, within_area w
        WHERE (p.search_name = lower(unaccent($1)) OR p.search_name % lower(unaccent($1)))
          AND ($3::text IS NULL OR p.kind = $3)
          AND (w.geom IS NULL OR ST_Intersects(p.geom, w.geom))
        ORDER BY exact DESC, array_position($4::text[], p.kind), score DESC
        LIMIT 12
        """;

    public async Task<PlaceResolution> ResolveAsync(IReadOnlyList<PlaceExpression> places, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(places);

        List<PlaceRef> resolved = [];
        List<string> unknown = [];
        List<PlaceChoice> ambiguous = [];
        foreach (PlaceExpression place in places.Where(place => !string.IsNullOrWhiteSpace(place.Name)))
        {
            List<PlaceCandidate> candidates = await LookupAsync(place, ct);
            if (candidates.Count == 0 && place.Kind is not null)
            {
                candidates = await LookupAsync(place with { Kind = null }, ct);
            }

            Classify(place, candidates, resolved, unknown, ambiguous);
        }

        logger.LogInformation(
            "Resolved {Resolved} places, {Unknown} unknown, {Ambiguous} ambiguous",
            resolved.Count,
            unknown.Count,
            ambiguous.Count);
        return new PlaceResolution(resolved, unknown, ambiguous);
    }

    private static void Classify(
        PlaceExpression place,
        List<PlaceCandidate> candidates,
        List<PlaceRef> resolved,
        List<string> unknown,
        List<PlaceChoice> ambiguous)
    {
        List<PlaceCandidate> best = BestCandidates(candidates);
        List<string> municipalities = best.Select(Municipality).Distinct().ToList();
        if (best.Count == 0)
        {
            unknown.Add(place.Name);
        }
        else if (municipalities.Count > 1)
        {
            ambiguous.Add(new PlaceChoice(place.Name, best.Select(candidate => $"{candidate.Name} ({Municipality(candidate)})").Distinct().ToList()));
        }
        else
        {
            resolved.AddRange(best.Select(candidate => new PlaceRef(candidate.Id, BufferMeters(place, candidate))));
        }
    }

    private static List<PlaceCandidate> BestCandidates(List<PlaceCandidate> candidates)
    {
        PlaceCandidate? top = candidates.FirstOrDefault();
        if (top is null || (!top.Exact && top.Score < MinimumFuzzyScore))
        {
            return [];
        }

        return top.Exact
            ? candidates.Where(candidate => candidate.Exact && candidate.Kind == top.Kind).ToList()
            : [top];
    }

    private static double BufferMeters(PlaceExpression place, PlaceCandidate candidate) =>
        place.NearKm is > 0 ? place.NearKm.Value * MetersPerKilometer
        : candidate.Kind == LakeKind ? DefaultLakeShoreMeters
        : 0;

    private static string Municipality(PlaceCandidate candidate) =>
        (candidate.Context ?? candidate.Name).Split(ContextSeparator)[0].Trim();

    private async Task<List<PlaceCandidate>> LookupAsync(PlaceExpression place, CancellationToken ct)
    {
        string? kindHint = SupportedKindHints.Contains(place.Kind) ? place.Kind : null;
        await using NpgsqlCommand command = dataSource.CreateCommand(LookupSql);
        command.Parameters.Add(new NpgsqlParameter { Value = place.Name.Trim() });
        command.Parameters.Add(new NpgsqlParameter { Value = (object?)place.Within?.Trim() ?? DBNull.Value, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { Value = (object?)kindHint ?? DBNull.Value, NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { Value = KindPriority });
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
        List<PlaceCandidate> candidates = [];
        while (await reader.ReadAsync(ct))
        {
            candidates.Add(new PlaceCandidate(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                await reader.IsDBNullAsync(3, ct) ? null : reader.GetString(3),
                reader.GetBoolean(4),
                reader.GetFloat(5)));
        }

        return candidates;
    }
}
