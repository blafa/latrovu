using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Latrovu.BuildingBlocks.Infrastructure.Persistence;

public static class PostgresServiceCollectionExtensions
{
    private const string DatabaseConnectionName = "Db";

    public static IServiceCollection AddPostgres(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string connectionString = configuration.GetConnectionString(DatabaseConnectionName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{DatabaseConnectionName} is missing");
        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        return services;
    }
}
