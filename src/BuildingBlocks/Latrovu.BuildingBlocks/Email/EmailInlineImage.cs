namespace Latrovu.BuildingBlocks.Email;

public sealed record EmailInlineImage(string ContentId, string FileName, string MediaType, byte[] Content);
