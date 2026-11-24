using System.Diagnostics;
using System.Text.Json;
using Npgsql;

namespace Latrovu.Listings.Infrastructure.Ingestion;

public sealed class ListingStore(NpgsqlDataSource dataSource, ILogger<ListingStore> logger) : IListingStore
{
    private const string InsertSql = """
        INSERT INTO listings (id, source_email, subject, file_name, data, extracted_data, field_scores, field_evidence, source_pages)
        VALUES ($1, $2, $3, $4, $5::jsonb, $6::jsonb, $7::jsonb, $8::jsonb, $9::jsonb)
        """;

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
        await command.ExecuteNonQueryAsync(ct);

        logger.LogDebug("Inserted listing {ListingId} in {ElapsedMs} ms", listingId, stopwatch.ElapsedMilliseconds);
        return listingId;
    }

    private static object ToDbValue(string? value) => (object?)value ?? DBNull.Value;
}
