using Latrovu.BuildingBlocks.Pdf;
using Microsoft.Extensions.DependencyInjection;

namespace Latrovu.BuildingBlocks.Infrastructure.Pdf;

public static class PdfServiceCollectionExtensions
{
    public static IServiceCollection AddPdfReading(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IPdfTextReader, PdfPigTextReader>();
        services.AddSingleton<IPdfImageReader, PdfPigImageReader>();
        return services;
    }
}
