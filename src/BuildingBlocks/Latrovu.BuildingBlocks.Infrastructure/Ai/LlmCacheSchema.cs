using System.Diagnostics;
using Latrovu.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Latrovu.BuildingBlocks.Infrastructure.Ai;

public sealed class LlmCacheSchema(NpgsqlDataSource dataSource, ILogger<LlmCacheSchema> logger) : IHostedService
{
    private const string CreateSql = """
        CREATE TABLE IF NOT EXISTS llm_response_cache (
            cache_key  char(64) PRIMARY KEY,
            model      text NOT NULL,
            response   text NOT NULL,
            created_at timestamptz NOT NULL DEFAULT now()
        );
        """;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        await using NpgsqlCommand command = dataSource.CreateCommand(SchemaLock.Wrap(CreateSql));
        await command.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("LLM response cache schema ensured in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
