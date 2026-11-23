using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Latrovu.Listings.Infrastructure;

public sealed class ListingsDatabaseSchema(NpgsqlDataSource dataSource, ILogger<ListingsDatabaseSchema> logger) : IHostedService
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
        ALTER TABLE listings ADD COLUMN IF NOT EXISTS extracted_data jsonb;
        ALTER TABLE listings ADD COLUMN IF NOT EXISTS field_scores jsonb;
        CREATE TABLE IF NOT EXISTS listing_images (
            listing_id   uuid NOT NULL REFERENCES listings (id) ON DELETE CASCADE,
            position     int NOT NULL,
            storage_key  text NOT NULL,
            content_type varchar(50) NOT NULL,
            created_at   timestamptz NOT NULL DEFAULT now(),
            PRIMARY KEY (listing_id, position)
        );
        CREATE TABLE IF NOT EXISTS listing_access_tokens (
            token_hash   char(64) PRIMARY KEY,
            listing_id   uuid NOT NULL REFERENCES listings (id) ON DELETE CASCADE,
            created_at   timestamptz NOT NULL DEFAULT now(),
            expires_at   timestamptz NOT NULL
        );
        CREATE TABLE IF NOT EXISTS listing_verification_codes (
            id           uuid PRIMARY KEY,
            listing_id   uuid NOT NULL REFERENCES listings (id) ON DELETE CASCADE,
            code_hash    char(64) NOT NULL,
            attempts     int NOT NULL DEFAULT 0,
            created_at   timestamptz NOT NULL,
            expires_at   timestamptz NOT NULL
        );
        CREATE INDEX IF NOT EXISTS ix_listing_verification_codes_listing ON listing_verification_codes (listing_id, created_at);
        CREATE TABLE IF NOT EXISTS listing_review_sessions (
            session_hash char(64) PRIMARY KEY,
            listing_id   uuid NOT NULL REFERENCES listings (id) ON DELETE CASCADE,
            expires_at   timestamptz NOT NULL
        );
        """;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        await using NpgsqlCommand command = dataSource.CreateCommand(CreateSchemaSql);
        await command.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("Listings schema ensured in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
