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
        ALTER TABLE listings ADD COLUMN IF NOT EXISTS field_evidence jsonb;
        ALTER TABLE listings ADD COLUMN IF NOT EXISTS publish_contact boolean NOT NULL DEFAULT false;
        CREATE EXTENSION IF NOT EXISTS postgis;
        ALTER TABLE listings ADD COLUMN IF NOT EXISTS location geometry(Point, 2056);
        ALTER TABLE listings ADD COLUMN IF NOT EXISTS location_precision text;
        ALTER TABLE listings ADD COLUMN IF NOT EXISTS source_pages jsonb;
        ALTER TABLE listings ADD COLUMN IF NOT EXISTS source_hash char(64);
        CREATE INDEX IF NOT EXISTS ix_listings_source ON listings (source_email, source_hash);
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
        CREATE TABLE IF NOT EXISTS processed_submissions (
            source_id    text PRIMARY KEY,
            processed_at timestamptz NOT NULL DEFAULT now()
        );
        CREATE TABLE IF NOT EXISTS listing_view_links (
            token_hash      char(64) PRIMARY KEY,
            listing_id      uuid NOT NULL REFERENCES listings (id) ON DELETE CASCADE,
            search_id       uuid,
            created_at      timestamptz NOT NULL,
            expires_at      timestamptz NOT NULL,
            first_viewed_at timestamptz,
            view_count      int NOT NULL DEFAULT 0
        );
        CREATE INDEX IF NOT EXISTS ix_listing_view_links_listing ON listing_view_links (listing_id);
        CREATE TABLE IF NOT EXISTS listing_chat_messages (
            id          bigserial PRIMARY KEY,
            token_hash  char(64) NOT NULL,
            listing_id  uuid NOT NULL REFERENCES listings (id) ON DELETE CASCADE,
            role        text NOT NULL,
            text        text NOT NULL,
            source      text,
            page        int,
            created_at  timestamptz NOT NULL
        );
        CREATE INDEX IF NOT EXISTS ix_listing_chat_messages_token ON listing_chat_messages (token_hash, created_at);
        ALTER TABLE listing_chat_messages ADD COLUMN IF NOT EXISTS forward_status text;
        ALTER TABLE listing_chat_messages ADD COLUMN IF NOT EXISTS forwarded_question text;
        ALTER TABLE listing_chat_messages ADD COLUMN IF NOT EXISTS forwarded_at timestamptz;
        ALTER TABLE listing_chat_messages ADD COLUMN IF NOT EXISTS forward_batch uuid;
        CREATE INDEX IF NOT EXISTS ix_listing_chat_messages_pending ON listing_chat_messages (forwarded_at) WHERE forward_status = 'pending';
        ALTER TABLE listing_chat_messages ADD COLUMN IF NOT EXISTS original_text text;
        ALTER TABLE listing_view_links ADD COLUMN IF NOT EXISTS seeker_address varchar(320);
        CREATE TABLE IF NOT EXISTS listing_knowledge (
            id          bigserial PRIMARY KEY,
            listing_id  uuid NOT NULL REFERENCES listings (id) ON DELETE CASCADE,
            question    text NOT NULL,
            answer      text NOT NULL,
            created_at  timestamptz NOT NULL
        );
        CREATE INDEX IF NOT EXISTS ix_listing_knowledge_listing ON listing_knowledge (listing_id);
        CREATE TABLE IF NOT EXISTS listing_viewing_slots (
            id          bigserial PRIMARY KEY,
            listing_id  uuid NOT NULL REFERENCES listings (id) ON DELETE CASCADE,
            starts_at   timestamptz NOT NULL,
            ends_at     timestamptz NOT NULL,
            capacity    int NOT NULL,
            created_at  timestamptz NOT NULL DEFAULT now()
        );
        CREATE INDEX IF NOT EXISTS ix_listing_viewing_slots_listing ON listing_viewing_slots (listing_id, starts_at);
        CREATE TABLE IF NOT EXISTS listing_viewing_bookings (
            slot_id     bigint NOT NULL REFERENCES listing_viewing_slots (id) ON DELETE CASCADE,
            token_hash  char(64) NOT NULL,
            created_at  timestamptz NOT NULL,
            PRIMARY KEY (slot_id, token_hash)
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
