namespace Latrovu.BuildingBlocks.Infrastructure.Persistence;

public static class SchemaLock
{
    private const long LockKey = 7_420_042;

    public static string Wrap(string ddl) => $"BEGIN; SELECT pg_advisory_xact_lock({LockKey}); {ddl} COMMIT;";
}
