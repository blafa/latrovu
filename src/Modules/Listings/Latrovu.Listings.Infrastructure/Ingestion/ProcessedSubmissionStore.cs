using Npgsql;

namespace Latrovu.Listings.Infrastructure.Ingestion;

public sealed class ProcessedSubmissionStore(NpgsqlDataSource dataSource) : IProcessedSubmissionStore
{
    private const string ExistsSql = "SELECT EXISTS (SELECT 1 FROM processed_submissions WHERE source_id = $1)";

    private const string InsertSql = "INSERT INTO processed_submissions (source_id) VALUES ($1) ON CONFLICT DO NOTHING";

    public async Task<bool> IsProcessedAsync(string sourceId, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(ExistsSql);
        command.Parameters.Add(new NpgsqlParameter { Value = sourceId });
        return await command.ExecuteScalarAsync(ct) is true;
    }

    public async Task MarkProcessedAsync(string sourceId, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(InsertSql);
        command.Parameters.Add(new NpgsqlParameter { Value = sourceId });
        await command.ExecuteNonQueryAsync(ct);
    }
}
