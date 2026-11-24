using System.Net.Http.Headers;
using Latrovu.BuildingBlocks.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Latrovu.BuildingBlocks.Infrastructure.Ai;

public static class AiSetup
{
    private const string BearerScheme = "Bearer";
    private const string CacheSettingKey = "AI:CacheResponses";

    public static IServiceCollection AddAiExtraction(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHttpClient<InfomaniakLlmClient>(ConfigureHttpClient);

        if (configuration.GetValue<bool>(CacheSettingKey))
        {
            services.AddHostedService<LlmCacheSchema>();
            services.AddScoped<ILlmClient, CachingLlmClient>();
        }
        else
        {
            services.AddScoped<ILlmClient>(provider => provider.GetRequiredService<InfomaniakLlmClient>());
        }

        return services;
    }

    private static void ConfigureHttpClient(IServiceProvider provider, HttpClient http)
    {
        AiOptions ai = provider.GetRequiredService<IOptions<AiOptions>>().Value;
        http.BaseAddress = new Uri(ai.BaseUrl);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerScheme, ai.Token);
    }
}
