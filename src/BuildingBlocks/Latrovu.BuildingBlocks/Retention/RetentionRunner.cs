namespace Latrovu.BuildingBlocks.Retention;

public static class RetentionRunner
{
    public static async Task<RetentionResult> RunAsync(IEnumerable<IRetentionTask> tasks, DateTimeOffset now, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        Dictionary<string, int> deleted = [];
        List<RetentionFailure> failures = [];
        foreach (IRetentionTask task in tasks)
        {
            try
            {
                deleted[task.Name] = await task.RunAsync(now, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failures.Add(new RetentionFailure(task.Name, ex));
            }
        }

        return new RetentionResult(deleted, failures);
    }
}
