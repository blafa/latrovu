using System.Text.Json.Nodes;

namespace Latrovu.Listings;

public static class OfferTypeConfidence
{
    public const string OfferTypePath = "offerType";

    private static readonly IReadOnlyDictionary<string, string[]> PriceFieldsByOfferType = new Dictionary<string, string[]>
    {
        ["sale"] = ["sale.priceChf"],
        ["rent"] = ["rent.netChf", "rent.grossChf"]
    };

    public static FieldConfidence FromPrice(JsonObject data, IReadOnlyDictionary<string, FieldConfidence> scores)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(scores);

        string? offerType = data[OfferTypePath] is JsonValue value && value.TryGetValue(out string? text) ? text : null;
        if (offerType is null || !PriceFieldsByOfferType.TryGetValue(offerType, out string[]? priceFields))
        {
            return FieldConfidence.Low;
        }

        return priceFields
            .Where(scores.ContainsKey)
            .Select(field => scores[field])
            .DefaultIfEmpty(FieldConfidence.Low)
            .Max();
    }
}
