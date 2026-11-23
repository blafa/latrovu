using Latrovu.Core.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Latrovu.Listings.Infrastructure;

public static class ListingsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddListingsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<ListingsDatabaseSchema>();
        services.AddSingleton<IListingStore, ListingStore>();
        services.AddSingleton<IListingImageStore, ListingImageStore>();
        services.AddSingleton<IListingReviewStore, ListingReviewStore>();
        services.AddSingleton<IListingAccessTokenStore, ListingAccessTokenStore>();
        services.AddSingleton<IVerificationCodeStore, VerificationCodeStore>();
        services.AddSingleton<IReviewSessionStore, ReviewSessionStore>();
        services.AddSingleton<IVerificationCodeSender, EmailVerificationCodeSender>();
        services.AddScoped<IDocumentClassifier, DocumentClassifier>();
        services.AddScoped<IListingExtractor, ListingExtractor>();
        services.AddScoped<IListingImageService, ListingImageService>();
        services.AddScoped<IListingImageCollector, ListingImageCollector>();
        services.AddScoped<IListingReviewService, ListingReviewService>();
        services.AddScoped<IReviewAccessService, ReviewAccessService>();
        services.AddScoped<IListingDocumentHandler, ListingIntake>();
        return services;
    }
}
