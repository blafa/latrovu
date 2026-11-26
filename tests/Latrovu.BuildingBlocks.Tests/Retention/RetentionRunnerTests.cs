using Latrovu.BuildingBlocks.Retention;

namespace Latrovu.BuildingBlocks.Tests.Retention;

public sealed class RetentionRunnerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Run_ReportsDeletedRowsPerTask()
    {
        RetentionResult result = await RetentionRunner.RunAsync(
            [new FakeRetentionTask("Listings", 12), new FakeRetentionTask("Searches", 3)],
            Now,
            CancellationToken.None);

        Assert.Equal(12, result.DeletedByTask["Listings"]);
        Assert.Equal(3, result.DeletedByTask["Searches"]);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public async Task Run_FailingTask_DoesNotStopTheOthers()
    {
        FakeRetentionTask after = new("Searches", 3);

        RetentionResult result = await RetentionRunner.RunAsync(
            [new FakeRetentionTask("Listings", 0, new InvalidOperationException("db down")), after],
            Now,
            CancellationToken.None);

        Assert.Equal("Listings", Assert.Single(result.Failures).Task);
        Assert.Equal(Now, after.RanAt);
        Assert.Equal(3, result.DeletedByTask["Searches"]);
    }
}
