namespace Latrovu.Listings.Ingestion;

public interface IProcessedSubmissionStore
{
    Task<bool> IsProcessedAsync(string sourceId, CancellationToken ct);

    Task MarkProcessedAsync(string sourceId, CancellationToken ct);
}
