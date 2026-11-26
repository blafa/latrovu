using Latrovu.BuildingBlocks.Retention;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Latrovu.BuildingBlocks.Infrastructure.Retention;

public sealed class RetentionWorker(
    IServiceScopeFactory scopes,
    TimeProvider timeProvider,
    ILogger<RetentionWorker> logger) : BackgroundService
{
    public static readonly TimeSpan RunInterval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Retention worker started, running every {Interval}", RunInterval);
        await RunOnceAsync(stoppingToken);
        using PeriodicTimer timer = new(RunInterval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        await using AsyncServiceScope scope = scopes.CreateAsyncScope();
        IEnumerable<IRetentionTask> tasks = scope.ServiceProvider.GetServices<IRetentionTask>();
        RetentionResult result = await RetentionRunner.RunAsync(tasks, timeProvider.GetUtcNow(), ct);
        foreach ((string task, int deleted) in result.DeletedByTask)
        {
            logger.LogInformation("Retention {Task}: {DeletedCount} rows deleted", task, deleted);
        }

        foreach (RetentionFailure failure in result.Failures)
        {
            logger.LogError(failure.Error, "Retention {Task} failed, retrying at the next run", failure.Task);
        }
    }
}
