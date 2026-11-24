using Latrovu.BuildingBlocks.Geo;
using Microsoft.Extensions.DependencyInjection;

namespace Latrovu.BuildingBlocks.Infrastructure.Geo;

public static class GeoSetup
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

    public static IServiceCollection AddGeocoding(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient<IGeocoder, SwisstopoGeocoder>(http =>
        {
            http.BaseAddress = new Uri(SwisstopoGeocoder.BaseUrl);
            http.Timeout = RequestTimeout;
        });
        return services;
    }
}
