using Latrovu.Listings.Renewal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Latrovu.Listings.Infrastructure.Renewal;

public sealed class ListingRenewalWorker(
    IServiceScopeFactory scopes,
    TimeProvider timeProvider,
    ILogger<ListingRenewalWorker> logger) : BackgroundService
{
    public static readonly TimeSpan RunInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Listing renewal worker started, running every {Interval}", RunInterval);
        await TryRunAsync(stoppingToken);
        using PeriodicTimer timer = new(RunInterval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await TryRunAsync(stoppingToken);
        }
    }

    private async Task TryRunAsync(CancellationToken ct)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<RenewalReminderService>().RunAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Renewal run failed, retrying at the next interval");
        }
    }
}
