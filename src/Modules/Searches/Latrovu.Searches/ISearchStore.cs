namespace Latrovu.Searches;

public interface ISearchStore
{
    Task<IReadOnlyList<Search>> FindBySeekerAsync(string seekerAddress, CancellationToken ct);

    Task SaveAsync(Search search, CancellationToken ct);

    Task<bool> IsProcessedAsync(string sourceId, CancellationToken ct);

    Task MarkProcessedAsync(string sourceId, CancellationToken ct);
}
