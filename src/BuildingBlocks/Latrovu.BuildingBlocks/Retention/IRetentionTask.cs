namespace Latrovu.BuildingBlocks.Retention;

public interface IRetentionTask
{
    string Name { get; }

    Task<int> RunAsync(DateTimeOffset now, CancellationToken ct);
}
