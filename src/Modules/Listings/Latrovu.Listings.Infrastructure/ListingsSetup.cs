using Latrovu.BuildingBlocks.Infrastructure.Messaging;
using Latrovu.Core.Submissions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Latrovu.Listings.Infrastructure;

public static class ListingsSetup
{
    public static IServiceCollection AddListingsReview(this IServiceCollection services)
    {
        AddListingsStorage(services);
        services.AddSingleton<IVerificationCodeSender, EmailVerificationCodeSender>();
        services.AddScoped<IListingReviewService, ListingReviewService>();
        services.AddScoped<IReviewAccessService, ReviewAccessService>();
        return services;
    }

    public static IServiceCollection AddListingsProcessing(this IServiceCollection services)
    {
        AddListingsStorage(services);
        services.AddScoped<IDocumentClassifier, DocumentClassifier>();
        services.AddScoped<IListingExtractor, ListingExtractor>();
        services.AddScoped<IListingImageService, ListingImageService>();
        services.AddScoped<IListingImageCollector, ListingImageCollector>();
        services.AddScoped<IListingIntake, ListingIntake>();
        services.AddMessageHandler<SubmissionReceived, SubmissionReceivedHandler>();
        return services;
    }

    private static void AddListingsStorage(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<ListingsDatabaseSchema>();
        services.TryAddSingleton<IListingStore, ListingStore>();
        services.TryAddSingleton<IListingImageStore, ListingImageStore>();
        services.TryAddSingleton<IListingReviewStore, ListingReviewStore>();
        services.TryAddSingleton<IListingAccessTokenStore, ListingAccessTokenStore>();
        services.TryAddSingleton<IVerificationCodeStore, VerificationCodeStore>();
        services.TryAddSingleton<IReviewSessionStore, ReviewSessionStore>();
        services.TryAddSingleton<IProcessedSubmissionStore, ProcessedSubmissionStore>();
    }
}
