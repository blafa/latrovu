namespace Latrovu.Api.Storage;

public sealed record SignedHeaders(string Authorization, string AmzDate, string ContentSha256);
