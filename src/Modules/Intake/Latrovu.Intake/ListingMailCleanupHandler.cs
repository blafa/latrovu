using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;

namespace Latrovu.Intake;

public sealed class ListingMailCleanupHandler(
    IMailInbox inbox,
    ILogger<ListingMailCleanupHandler> logger) : IMessageHandler<ListingWithdrawn>
{
    public async Task HandleAsync(ListingWithdrawn message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        IReadOnlyList<string> messageIds = message.SourceMessageIds ?? [];
        if (messageIds.Count == 0)
        {
            logger.LogInformation("Listing {ListingId} withdrawn without known mails, nothing to delete", message.ListingId);
            return;
        }

        int deleted = await inbox.DeleteProcessedAsync(messageIds, ct);
        logger.LogInformation("Listing {ListingId}: {DeletedCount} mails deleted from the mailbox", message.ListingId, deleted);
    }
}
