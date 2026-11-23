namespace Latrovu.Core.Messaging;

public interface IListingDocumentHandler
{
    Task HandleAsync(ReceivedMessage message, IReadOnlyList<ReceivedFile> files, CancellationToken ct);
}
