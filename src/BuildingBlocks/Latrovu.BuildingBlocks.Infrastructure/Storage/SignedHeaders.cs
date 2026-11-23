namespace Latrovu.BuildingBlocks.Infrastructure.Storage;

public sealed record SignedHeaders(string Authorization, string AmzDate, string ContentSha256);
