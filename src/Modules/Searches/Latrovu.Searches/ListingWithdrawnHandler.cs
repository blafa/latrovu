using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;

namespace Latrovu.Searches;

public sealed class ListingWithdrawnHandler(
    ISearchStore searches,
    ILogger<ListingWithdrawnHandler> logger) : IMessageHandler<ListingWithdrawn>
{
    public async Task HandleAsync(ListingWithdrawn message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        await searches.RemoveCandidateAsync(message.ListingId, ct);
        await searches.RemoveAlertsAsync(message.ListingId, ct);
        logger.LogInformation("Listing {ListingId} removed from matching candidates and alerts", message.ListingId);
    }
}
