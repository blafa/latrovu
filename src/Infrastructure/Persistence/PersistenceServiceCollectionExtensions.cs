using Latrovu.Core.Listings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Latrovu.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    private const string DatabaseConnectionName = "Db";

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string connectionString = configuration.GetConnectionString(DatabaseConnectionName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{DatabaseConnectionName} is missing");

        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<DatabaseSchema>();
        services.AddSingleton<IListingStore, ListingStore>();
        services.AddSingleton<IListingImageStore, ListingImageStore>();
        return services;
    }
}
