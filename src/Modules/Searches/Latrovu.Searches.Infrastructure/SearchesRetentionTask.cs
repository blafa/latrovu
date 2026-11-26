using Latrovu.BuildingBlocks.Retention;
using Npgsql;

namespace Latrovu.Searches.Infrastructure;

public sealed class SearchesRetentionTask(NpgsqlDataSource dataSource) : IRetentionTask
{
    public static readonly TimeSpan StoppedSearchRetention = TimeSpan.FromDays(7);
    public static readonly TimeSpan ProcessedIdRetention = TimeSpan.FromDays(7);

    private const string StoppedSearchesSql = "DELETE FROM searches WHERE status = $2 AND updated_at < $1";

    private const string ProcessedRequestsSql = "DELETE FROM processed_search_requests WHERE processed_at < $1";

    public string Name => "Searches";

    public async Task<int> RunAsync(DateTimeOffset now, CancellationToken ct)
    {
        int deleted = await DeleteAsync(StoppedSearchesSql, now - StoppedSearchRetention, Search.StoppedStatus, ct);
        deleted += await DeleteAsync(ProcessedRequestsSql, now - ProcessedIdRetention, null, ct);
        return deleted;
    }

    private async Task<int> DeleteAsync(string sql, DateTimeOffset cutoff, string? status, CancellationToken ct)
    {
        await using NpgsqlCommand command = dataSource.CreateCommand(sql);
        command.Parameters.Add(new NpgsqlParameter { Value = cutoff });
        if (status is not null)
        {
            command.Parameters.Add(new NpgsqlParameter { Value = status });
        }

        return await command.ExecuteNonQueryAsync(ct);
    }
}
