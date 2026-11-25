using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Latrovu.BuildingBlocks.Infrastructure.Messaging;

public sealed class OutboxCleanupWorker(
    NpgsqlDataSource dataSource,
    TimeProvider timeProvider,
    ILogger<OutboxCleanupWorker> logger) : BackgroundService
{
    public static readonly TimeSpan Retention = TimeSpan.FromDays(7);
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(6);

    private const string DeleteSql = "DELETE FROM outbox WHERE sent_at IS NOT NULL AND sent_at < $1";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox cleanup started, keeping delivered messages for {Retention}", Retention);
        await TryCleanAsync(stoppingToken);
        using PeriodicTimer timer = new(RunInterval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await TryCleanAsync(stoppingToken);
        }
    }

    private async Task TryCleanAsync(CancellationToken ct)
    {
        try
        {
            await using NpgsqlCommand command = dataSource.CreateCommand(DeleteSql);
            command.Parameters.Add(new NpgsqlParameter { Value = timeProvider.GetUtcNow() - Retention });
            int deleted = await command.ExecuteNonQueryAsync(ct);
            logger.LogInformation("Outbox cleanup removed {DeletedCount} delivered messages", deleted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Outbox cleanup failed, retrying at the next interval");
        }
    }
}
