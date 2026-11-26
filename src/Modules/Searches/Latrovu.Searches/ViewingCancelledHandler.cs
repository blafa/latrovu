using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;

namespace Latrovu.Searches;

public sealed class ViewingCancelledHandler(
    ISearchStore searches,
    ISeekerNotifier notifier,
    ILogger<ViewingCancelledHandler> logger) : IMessageHandler<ViewingCancelled>
{
    public async Task HandleAsync(ViewingCancelled message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        Search? search = await searches.FindByIdAsync(message.SearchId, ct);
        if (search is null)
        {
            logger.LogWarning("Viewing cancellation for listing {ListingId} belongs to unknown search {SearchId}, no confirmation sent", message.ListingId, message.SearchId);
            return;
        }

        string? reference = await notifier.SendViewingConfirmationAsync(
            new SeekerViewingConfirmation(search.SeekerAddress, search.Channel, search.Language, message.ListingTitle, message.StartsAt, message.EndsAt, IsCancellation: true),
            ct);
        if (reference is not null)
        {
            await searches.RecordSentMessageAsync(reference, search.Id, ct);
        }

        logger.LogInformation("Viewing cancellation for listing {ListingId} confirmed to search {SearchId}", message.ListingId, message.SearchId);
    }
}
