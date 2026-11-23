namespace Latrovu.BuildingBlocks.Messaging;

public interface IMessageHandler<in TMessage>
{
    Task HandleAsync(TMessage message, CancellationToken ct);
}
