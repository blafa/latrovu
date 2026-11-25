using Latrovu.BuildingBlocks.Storage;

namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeObjectStorage : IObjectStorage
{
    public List<string> DeletedKeys { get; } = [];

    public Task PutAsync(string key, byte[] content, string contentType, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<StoredObject?> GetAsync(string key, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        DeletedKeys.Add(key);
        return Task.CompletedTask;
    }
}
