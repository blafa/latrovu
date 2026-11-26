using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Latrovu.BuildingBlocks.Infrastructure.Retention;

public static class RetentionSetup
{
    public static IServiceCollection AddRetention(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<RetentionWorker>();
        return services;
    }
}
