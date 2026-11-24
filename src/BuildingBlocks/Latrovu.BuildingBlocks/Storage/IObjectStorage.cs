namespace Latrovu.BuildingBlocks.Storage;

public interface IObjectStorage
{
    Task PutAsync(string key, byte[] content, string contentType, CancellationToken ct);

    Task<StoredObject?> GetAsync(string key, CancellationToken ct);

    Task DeleteAsync(string key, CancellationToken ct);
}
