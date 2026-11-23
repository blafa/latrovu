using System.Diagnostics;
using Npgsql;

namespace Latrovu.Api.Data;

public sealed class DatabaseSchema(NpgsqlDataSource dataSource, ILogger<DatabaseSchema> logger)
{
    private const string CreateSchemaSql = """
        CREATE TABLE IF NOT EXISTS listings (
            id           uuid PRIMARY KEY,
            created_at   timestamptz NOT NULL DEFAULT now(),
            status       varchar(20) NOT NULL DEFAULT 'draft',
            source_email varchar(320) NOT NULL,
            subject      text,
            file_name    text,
            data         jsonb
        );
        CREATE INDEX IF NOT EXISTS ix_listings_status ON listings (status);
        CREATE TABLE IF NOT EXISTS listing_images (
            listing_id   uuid NOT NULL REFERENCES listings (id) ON DELETE CASCADE,
            position     int NOT NULL,
            storage_key  text NOT NULL,
            content_type varchar(50) NOT NULL,
            created_at   timestamptz NOT NULL DEFAULT now(),
            PRIMARY KEY (listing_id, position)
        );
        """;

    public async Task EnsureCreatedAsync(CancellationToken ct = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        await using NpgsqlCommand command = dataSource.CreateCommand(CreateSchemaSql);
        await command.ExecuteNonQueryAsync(ct);
        logger.LogInformation("Database schema ensured in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
    }
}
