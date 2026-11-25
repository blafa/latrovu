using System.Threading.RateLimiting;
using Latrovu.BuildingBlocks.Infrastructure.Messaging;
using Latrovu.Core.Listings;
using Latrovu.Core.Submissions;
using Latrovu.Listings.Infrastructure.Matching;
using Latrovu.Listings.Infrastructure.Viewing.Access;
using Latrovu.Listings.Infrastructure.Viewing.Chat;
using Latrovu.Listings.Infrastructure.Viewing.Viewings;
using Latrovu.Listings.Matching;
using Latrovu.Listings.Viewing.Access;
using Latrovu.Listings.Viewing.Chat;
using Latrovu.Listings.Viewing.Viewings;
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
    private const int ImageRequestsPerMinute = 1200;
    private const string UnknownClient = "unknown";

    public static IServiceCollection AddListingsReview(this IServiceCollection services)
    {
        AddListingsStorage(services);
        services.AddSingleton<IVerificationCodeSender, EmailVerificationCodeSender>();
        services.AddScoped<IListingReviewService, ListingReviewService>();
        services.AddScoped<IReviewAccessService, ReviewAccessService>();
        services.AddScoped<AgentViewingService>();
        return services;
    }

    public static IServiceCollection AddListingsViewing(this IServiceCollection services)
    {
        AddListingsStorage(services);
        services.TryAddSingleton<IListingViewLinkStore, ListingViewLinkStore>();
        services.TryAddSingleton<IListingChatStore, ListingChatStore>();
        services.TryAddSingleton<IListingSourceTextStore, ListingSourceTextStore>();
        services.AddScoped<IListingChatAnswerer, ListingChatAnswerer>();
        services.AddScoped<ListingChatService>();
        services.AddScoped<IViewingNotifier, EmailViewingNotifier>();
        services.AddScoped<SeekerViewingService>();
        services.AddScoped<IListingViewService, ListingViewService>();
        services.TryAddSingleton<IListingAccessRequestStore, ListingAccessRequestStore>();
        services.AddScoped<ListingAccessService>();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(ListingViewEndpoints.RateLimitPolicy, context => PerClientLimit(context, ViewRequestsPerMinute));
            options.AddPolicy(ListingViewEndpoints.ImageRateLimitPolicy, context => PerClientLimit(context, ImageRequestsPerMinute));
        });
        return services;
    }

    public static IServiceCollection AddListingsProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        AddListingsStorage(services);
        services.AddOptions<QuestionForwardingOptions>()
            .Bind(configuration.GetSection(QuestionForwardingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHostedService<QuestionForwardingService>();
        services.AddScoped<IAgentReplyStore, AgentReplyStore>();
        services.AddScoped<IAnswerTranslator, LlmAnswerTranslator>();
        services.AddMessageHandler<AgentReplyReceived, AgentReplyHandler>();
        services.AddScoped<IDocumentClassifier, DocumentClassifier>();
        services.AddScoped<IListingExtractor, ListingExtractor>();
        services.AddScoped<IListingImageService, ListingImageService>();
        services.AddScoped<IListingImageCollector, ListingImageCollector>();
        services.AddScoped<IListingIntake, ListingIntake>();
        services.TryAddSingleton<IListingLocationStore, ListingLocationStore>();
        services.TryAddSingleton<IListingViewLinkStore, ListingViewLinkStore>();
        services.AddMessageHandler<SubmissionReceived, SubmissionReceivedHandler>();
        services.AddMessageHandler<ListingApproved, ListingApprovedHandler>();
        services.AddMessageHandler<ViewLinkRequested, ViewLinkRequestedHandler>();
        return services;
    }

    private static RateLimitPartition<string> PerClientLimit(HttpContext context, int permitsPerMinute) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient,
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitsPerMinute, Window = TimeSpan.FromMinutes(1) });

    private static void AddListingsStorage(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ListingsDatabaseSchema>());
        services.TryAddSingleton<IListingStore, ListingStore>();
        services.TryAddSingleton<IListingSourceMailStore, ListingSourceMailStore>();
        services.TryAddSingleton<IListingImageStore, ListingImageStore>();
        services.TryAddSingleton<IListingReviewStore, ListingReviewStore>();
        services.TryAddSingleton<IListingAccessTokenStore, ListingAccessTokenStore>();
        services.TryAddSingleton<IVerificationCodeStore, VerificationCodeStore>();
        services.TryAddSingleton<IReviewSessionStore, ReviewSessionStore>();
        services.TryAddSingleton<IProcessedSubmissionStore, ProcessedSubmissionStore>();
        services.TryAddSingleton<IListingKnowledgeStore, ListingKnowledgeStore>();
        services.TryAddSingleton<IViewingStore, ViewingStore>();
    }
}
