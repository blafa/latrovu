using System.Net.Http.Headers;
using Latrovu.BuildingBlocks.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Latrovu.BuildingBlocks.Infrastructure.Ai;

public static class AiServiceCollectionExtensions
{
    private const string BearerScheme = "Bearer";

    public static IServiceCollection AddAiExtraction(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHttpClient<ILlmClient, InfomaniakLlmClient>(ConfigureHttpClient);
        return services;
    }

    private static void ConfigureHttpClient(IServiceProvider provider, HttpClient http)
    {
        AiOptions ai = provider.GetRequiredService<IOptions<AiOptions>>().Value;
        http.BaseAddress = new Uri(ai.BaseUrl);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerScheme, ai.Token);
    }
}
