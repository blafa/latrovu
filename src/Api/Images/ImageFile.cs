namespace Latrovu.Api.Images;

public sealed record ImageFile(byte[] Content, string ContentType, string Extension);
