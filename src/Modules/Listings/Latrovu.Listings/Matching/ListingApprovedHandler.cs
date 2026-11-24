using System.Text.Json.Nodes;
using Latrovu.BuildingBlocks.Geo;
using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;

namespace Latrovu.Listings.Matching;

public sealed class ListingApprovedHandler(
    IListingReviewStore listings,
    IGeocoder geocoder,
    IListingLocationStore locations,
    ILogger<ListingApprovedHandler> logger) : IMessageHandler<ListingApproved>
{
    public async Task HandleAsync(ListingApproved message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        ListingReview? listing = await listings.GetAsync(message.ListingId, ct);
        if (listing is not { IsApproved: true })
        {
            logger.LogInformation("Listing {ListingId} is no longer approved, skipping matching", message.ListingId);
            return;
        }

        JsonObject data = JsonNode.Parse(listing.DataJson) as JsonObject ?? [];
        GeoPoint? point = await geocoder.GeocodeAsync(ListingMatchFacts.AddressOf(data), ct);
        if (point is null)
        {
            logger.LogWarning("Listing {ListingId} could not be geocoded and will not be matched", message.ListingId);
            return;
        }

        await locations.SaveAsync(message.ListingId, point, ListingMatchFacts.Announce(message.ListingId, data, point), ct);
        logger.LogInformation("Listing {ListingId} located ({Precision}) and announced for matching", message.ListingId, point.Precision);
    }
}
