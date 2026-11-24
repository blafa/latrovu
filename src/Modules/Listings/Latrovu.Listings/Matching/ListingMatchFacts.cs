using System.Text.Json.Nodes;
using Latrovu.BuildingBlocks.Geo;
using Latrovu.Core.Listings;

namespace Latrovu.Listings.Matching;

public static class ListingMatchFacts
{
    public static PostalAddress AddressOf(JsonObject data) =>
        new(Text(data, "location.street"), Text(data, "location.houseNumber"), Text(data, "location.zip"), Text(data, "location.city"));

    public static ListingReadyForMatching Announce(Guid listingId, JsonObject data, GeoPoint point)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(point);

        return new ListingReadyForMatching(
            listingId,
            point.East,
            point.North,
            point.Precision,
            Text(data, "offerType"),
            Number(data, "size.rooms"),
            Number(data, "rent.grossChf") ?? Number(data, "rent.netChf") ?? Number(data, "sale.priceChf"),
            Number(data, "size.livingAreaM2"),
            Text(data, "availability.availableFrom"),
            Text(data, "title"),
            Text(data, "location.city"));
    }

    private static string? Text(JsonObject data, string path) =>
        JsonPaths.Get(data, path) is JsonValue value && value.TryGetValue(out string? text) && !string.IsNullOrWhiteSpace(text)
            ? text.Trim()
            : JsonPaths.Get(data, path) is JsonValue number && number.TryGetValue(out decimal digits)
                ? digits.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : null;

    private static decimal? Number(JsonObject data, string path) =>
        JsonPaths.Get(data, path) is JsonValue value && value.TryGetValue(out decimal number) ? number : null;
}
