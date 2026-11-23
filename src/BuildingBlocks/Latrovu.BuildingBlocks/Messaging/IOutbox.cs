using System.Data.Common;

namespace Latrovu.BuildingBlocks.Messaging;

public interface IOutbox
{
    Task AddAsync(IEnumerable<object> messages, DbTransaction transaction, CancellationToken ct);
}
