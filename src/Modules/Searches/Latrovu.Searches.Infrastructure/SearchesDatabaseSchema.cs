using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Latrovu.Searches.Infrastructure;

public sealed class SearchesDatabaseSchema(NpgsqlDataSource dataSource, ILogger<SearchesDatabaseSchema> logger) : IHostedService
{
    private const string CreateSql = """
        CREATE TABLE IF NOT EXISTS searches (
            id             uuid PRIMARY KEY,
            seeker_address text NOT NULL,
            channel        text NOT NULL,
            name           text NOT NULL,
            language       text NOT NULL,
            criteria       jsonb NOT NULL,
            status         text NOT NULL,
            created_at     timestamptz NOT NULL DEFAULT now(),
            updated_at     timestamptz NOT NULL DEFAULT now()
        );
        CREATE INDEX IF NOT EXISTS ix_searches_seeker ON searches (seeker_address);
        CREATE TABLE IF NOT EXISTS processed_search_requests (
            source_id    text PRIMARY KEY,
            processed_at timestamptz NOT NULL DEFAULT now()
        );
        """;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        await using NpgsqlCommand command = dataSource.CreateCommand(CreateSql);
        await command.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("Searches schema ensured in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
