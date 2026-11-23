using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Latrovu.BuildingBlocks.Infrastructure.Messaging;

public sealed class OutboxSchema(NpgsqlDataSource dataSource, ILogger<OutboxSchema> logger) : IHostedService
{
    private const string CreateSql = """
        CREATE TABLE IF NOT EXISTS outbox (
            id          uuid PRIMARY KEY,
            type        text NOT NULL,
            payload     jsonb NOT NULL,
            created_at  timestamptz NOT NULL,
            sent_at     timestamptz
        );
        CREATE INDEX IF NOT EXISTS ix_outbox_unsent ON outbox (created_at) WHERE sent_at IS NULL;
        """;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        await using NpgsqlCommand command = dataSource.CreateCommand(CreateSql);
        await command.ExecuteNonQueryAsync(cancellationToken);
        logger.LogInformation("Outbox schema ensured in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
