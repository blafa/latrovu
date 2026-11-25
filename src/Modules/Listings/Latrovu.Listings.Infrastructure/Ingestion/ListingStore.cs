using System.Diagnostics;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace Latrovu.Listings.Infrastructure.Ingestion;

public sealed class ListingStore(NpgsqlDataSource dataSource, ILogger<ListingStore> logger) : IListingStore
{
    private const string InsertSql = """
        INSERT INTO listings (id, source_email, subject, file_name, data, extracted_data, field_scores, field_evidence, source_pages, source_hash)
        VALUES ($1, $2, $3, $4, $5::jsonb, $6::jsonb, $7::jsonb, $8::jsonb, $9::jsonb, $10)
        """;

    private const string FindSql = """
        SELECT id FROM listings
        WHERE lower(source_email) = lower($1) AND source_hash = $2 AND status IN ('draft', 'approved')
        ORDER BY created_at DESC LIMIT 1
        """;

    private const string FillPagesSql = "UPDATE listings SET source_pages = $2::jsonb WHERE id = $1 AND source_pages IS NULL";

    public async Task<Guid> AddAsync(NewListing listing, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(listing);

        Guid listingId = Guid.NewGuid();
        Stopwatch stopwatch = Stopwatch.StartNew();
        await using NpgsqlCommand command = dataSource.CreateCommand(InsertSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = listing.SourceEmail });
        command.Parameters.Add(new NpgsqlParameter { Value = ToDbValue(listing.Subject) });
        command.Parameters.Add(new NpgsqlParameter { Value = ToDbValue(listing.FileName) });
        command.Parameters.Add(new NpgsqlParameter { Value = ToDbValue(listing.Extraction?.DataJson) });
        command.Parameters.Add(new NpgsqlParameter { Value = ToDbValue(listing.Extraction?.ExtractedJson) });
        command.Parameters.Add(new NpgsqlParameter { Value = ToDbValue(listing.Extraction?.FieldScoresJson) });
        command.Parameters.Add(new NpgsqlParameter { Value = ToDbValue(listing.Extraction?.FieldEvidenceJson) });
        command.Parameters.Add(new NpgsqlParameter { Value = JsonSerializer.Serialize(listing.SourcePages) });
        command.Parameters.Add(new NpgsqlParameter { Value = ToDbValue(listing.SourceHash), NpgsqlDbType = NpgsqlDbType.Char });
        await command.ExecuteNonQueryAsync(ct);

        logger.LogDebug("Inserted listing {ListingId} in {ElapsedMs} ms", listingId, stopwatch.ElapsedMilliseconds);
        return listingId;
    }

    public async Task<Guid?> FindActiveBySourceAsync(string sourceEmail, string sourceHash, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(FindSql);
        command.Parameters.Add(new NpgsqlParameter { Value = sourceEmail });
        command.Parameters.Add(new NpgsqlParameter { Value = sourceHash, NpgsqlDbType = NpgsqlDbType.Char });
        return await command.ExecuteScalarAsync(ct) is Guid listingId ? listingId : null;
    }

    public async Task FillMissingSourcePagesAsync(Guid listingId, IReadOnlyList<string> pages, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(FillPagesSql);
        command.Parameters.Add(new NpgsqlParameter { Value = listingId });
        command.Parameters.Add(new NpgsqlParameter { Value = JsonSerializer.Serialize(pages) });
        await command.ExecuteNonQueryAsync(ct);
    }

    private static object ToDbValue(string? value) => (object?)value ?? DBNull.Value;
}
