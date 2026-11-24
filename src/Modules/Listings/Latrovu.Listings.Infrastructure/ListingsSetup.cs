using System.Threading.RateLimiting;
using Latrovu.BuildingBlocks.Infrastructure.Messaging;
using Latrovu.Core.Submissions;
using Latrovu.Listings.Infrastructure.Viewing.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Latrovu.Listings.Infrastructure;

public static class ListingsSetup
{
    private const int ViewRequestsPerMinute = 120;
    private const string UnknownClient = "unknown";

    public static IServiceCollection AddListingsReview(this IServiceCollection services)
    {
        AddListingsStorage(services);
        services.AddSingleton<IVerificationCodeSender, EmailVerificationCodeSender>();
        services.AddScoped<IListingReviewService, ListingReviewService>();
        services.AddScoped<IReviewAccessService, ReviewAccessService>();
        return services;
    }

    public static IServiceCollection AddListingsViewing(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        AddListingsStorage(services);
        services.AddOptions<ViewingOptions>()
            .Bind(configuration.GetSection(ViewingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.TryAddSingleton<IListingViewLinkStore, ListingViewLinkStore>();
        services.AddScoped<IListingViewService, ListingViewService>();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(ListingViewEndpoints.RateLimitPolicy, PerClientLimit);
        });
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

    private static RateLimitPartition<string> PerClientLimit(HttpContext context) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient,
            _ => new FixedWindowRateLimiterOptions { PermitLimit = ViewRequestsPerMinute, Window = TimeSpan.FromMinutes(1) });

    private static void AddListingsStorage(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ListingsDatabaseSchema>());
        services.TryAddSingleton<IListingStore, ListingStore>();
        services.TryAddSingleton<IListingImageStore, ListingImageStore>();
        services.TryAddSingleton<IListingReviewStore, ListingReviewStore>();
        services.TryAddSingleton<IListingAccessTokenStore, ListingAccessTokenStore>();
        services.TryAddSingleton<IVerificationCodeStore, VerificationCodeStore>();
        services.TryAddSingleton<IReviewSessionStore, ReviewSessionStore>();
        services.TryAddSingleton<IProcessedSubmissionStore, ProcessedSubmissionStore>();
    }
}
