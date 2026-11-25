using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Latrovu.Intake.Infrastructure;

public sealed class MailPollingService(
    IMailInbox inbox,
    IServiceScopeFactory scopeFactory,
    IOptions<MailOptions> options,
    ILogger<MailPollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = TimeSpan.FromSeconds(options.Value.PollIntervalSeconds);
        logger.LogInformation("Mail polling started with interval {Interval}", interval);

        using PeriodicTimer timer = new(interval);
        do
        {
            await PollOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Mail polling stopping");
        await base.StopAsync(cancellationToken);
    }

    private async Task PollOnceAsync(CancellationToken ct)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IMailDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<IMailDispatcher>();
            await inbox.ProcessPendingAsync(dispatcher.DispatchAsync, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Mail polling failed");
        }
    }
}
