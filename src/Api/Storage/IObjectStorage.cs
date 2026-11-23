namespace Latrovu.Api.Storage;

public interface IObjectStorage
{
    Task PutAsync(string key, byte[] content, string contentType, CancellationToken ct);
}
