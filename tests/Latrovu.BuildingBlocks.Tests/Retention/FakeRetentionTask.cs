using Latrovu.BuildingBlocks.Retention;

namespace Latrovu.BuildingBlocks.Tests.Retention;

internal sealed class FakeRetentionTask(string name, int deleted, Exception? failure = null) : IRetentionTask
{
    public DateTimeOffset? RanAt { get; private set; }

    public string Name => name;

    public Task<int> RunAsync(DateTimeOffset now, CancellationToken ct)
    {
        RanAt = now;
        return failure is null ? Task.FromResult(deleted) : Task.FromException<int>(failure);
    }
}
