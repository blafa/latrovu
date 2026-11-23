using Latrovu.Core.Listings;
using Latrovu.Core.Mail;
using Microsoft.Extensions.DependencyInjection;

namespace Latrovu.Core;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IMailRouter, MailRouter>();
        services.AddScoped<IMailDispatcher, MailDispatcher>();
        services.AddScoped<IListingIntake, ListingIntake>();
        services.AddScoped<IDocumentClassifier, DocumentClassifier>();
        services.AddScoped<IListingExtractor, ListingExtractor>();
        services.AddScoped<IListingImageService, ListingImageService>();
        services.AddScoped<IListingImageCollector, ListingImageCollector>();
        return services;
    }
}
