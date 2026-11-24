using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;

namespace Latrovu.Listings.Viewing;

public sealed class ViewLinkRequestedHandler(
    IListingViewLinkStore links,
    ILogger<ViewLinkRequestedHandler> logger) : IMessageHandler<ViewLinkRequested>
{
    public async Task HandleAsync(ViewLinkRequested message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        await links.CreateFromHashAsync(message.ListingId, message.SearchId, message.TokenHash, message.ExpiresAt, ct);
        logger.LogInformation("View link stored for listing {ListingId} and search {SearchId}", message.ListingId, message.SearchId);
    }
}
