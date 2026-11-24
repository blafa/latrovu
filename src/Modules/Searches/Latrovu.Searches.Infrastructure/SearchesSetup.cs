using Latrovu.BuildingBlocks.Infrastructure.Messaging;
using Latrovu.Core.Listings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Latrovu.Searches.Infrastructure;

public static class SearchesSetup
{
    public static IServiceCollection AddSearchesModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AlertOptions>()
            .Bind(configuration.GetSection(AlertOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, SearchesDatabaseSchema>());
        services.AddSingleton<ISearchStore, SearchStore>();
        services.AddSingleton<IPlaceResolver, PostgresPlaceResolver>();
        services.AddScoped<ISearchInterpreter, SearchInterpreter>();
        services.AddScoped<ISeekerNotifier, EmailSeekerNotifier>();
        services.AddMessageHandler<SearchRequestReceived, SearchRequestHandler>();
        services.AddMessageHandler<ListingReadyForMatching, ListingMatchingHandler>();
        return services;
    }
}
