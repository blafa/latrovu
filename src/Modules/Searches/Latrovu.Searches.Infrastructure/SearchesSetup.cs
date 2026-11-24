using Latrovu.BuildingBlocks.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Latrovu.Searches.Infrastructure;

public static class SearchesSetup
{
    public static IServiceCollection AddSearchesModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, SearchesDatabaseSchema>());
        services.AddSingleton<ISearchStore, SearchStore>();
        services.AddSingleton<IPlaceResolver, PostgresPlaceResolver>();
        services.AddScoped<ISearchInterpreter, SearchInterpreter>();
        services.AddScoped<ISeekerNotifier, EmailSeekerNotifier>();
        services.AddMessageHandler<SearchRequestReceived, SearchRequestHandler>();
        return services;
    }
}
