namespace Latrovu.BuildingBlocks.Retention;

public sealed record RetentionResult(IReadOnlyDictionary<string, int> DeletedByTask, IReadOnlyList<RetentionFailure> Failures);
