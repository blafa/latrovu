using Latrovu.BuildingBlocks.Messaging;
using Rebus.Handlers;

namespace Latrovu.BuildingBlocks.Infrastructure.Messaging;

public sealed class RebusMessageHandler<TMessage>(IMessageHandler<TMessage> handler) : IHandleMessages<TMessage>
{
    public Task Handle(TMessage message) => handler.HandleAsync(message, CancellationToken.None);
}
