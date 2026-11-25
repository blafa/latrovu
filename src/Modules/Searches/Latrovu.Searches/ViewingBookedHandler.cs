using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;

namespace Latrovu.Searches;

public sealed class ViewingBookedHandler(
    ISearchStore searches,
    ISeekerNotifier notifier,
    ILogger<ViewingBookedHandler> logger) : IMessageHandler<ViewingBooked>
{
    public async Task HandleAsync(ViewingBooked message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        Search? search = await searches.FindByIdAsync(message.SearchId, ct);
        if (search is null)
        {
            logger.LogWarning("Viewing booking for listing {ListingId} belongs to unknown search {SearchId}, no confirmation sent", message.ListingId, message.SearchId);
            return;
        }

        string? reference = await notifier.SendViewingConfirmationAsync(
            new SeekerViewingConfirmation(search.SeekerAddress, search.Channel, search.Language, message.ListingTitle, message.StartsAt, message.EndsAt),
            ct);
        if (reference is not null)
        {
            await searches.RecordSentMessageAsync(reference, search.Id, ct);
        }

        logger.LogInformation("Viewing confirmation for listing {ListingId} sent to search {SearchId}", message.ListingId, message.SearchId);
    }
}
