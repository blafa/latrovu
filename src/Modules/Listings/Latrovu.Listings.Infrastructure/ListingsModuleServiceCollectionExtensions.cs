using Latrovu.Core.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Latrovu.Listings.Infrastructure;

public static class ListingsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddListingsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostedService<ListingsDatabaseSchema>();
        services.AddSingleton<IListingStore, ListingStore>();
        services.AddSingleton<IListingImageStore, ListingImageStore>();
        services.AddScoped<IDocumentClassifier, DocumentClassifier>();
        services.AddScoped<IListingExtractor, ListingExtractor>();
        services.AddScoped<IListingImageService, ListingImageService>();
        services.AddScoped<IListingImageCollector, ListingImageCollector>();
        services.AddScoped<IListingDocumentHandler, ListingIntake>();
        return services;
    }
}
