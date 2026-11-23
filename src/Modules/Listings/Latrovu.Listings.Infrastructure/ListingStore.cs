using System.Diagnostics;
using Latrovu.Listings;
using Npgsql;

namespace Latrovu.Listings.Infrastructure;

public sealed class ListingStore(NpgsqlDataSource dataSource, ILogger<ListingStore> logger) : IListingStore
{
    private const string InsertSql =
        "INSERT INTO listings (id, source_email, subject, file_name, data) VALUES ($1, $2, $3, $4, $5::jsonb)";

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
        command.Parameters.Add(new NpgsqlParameter { Value = ToDbValue(listing.Data) });
        await command.ExecuteNonQueryAsync(ct);

        logger.LogDebug("Inserted listing {ListingId} in {ElapsedMs} ms", listingId, stopwatch.ElapsedMilliseconds);
        return listingId;
    }

    private static object ToDbValue(string? value) => (object?)value ?? DBNull.Value;
}
