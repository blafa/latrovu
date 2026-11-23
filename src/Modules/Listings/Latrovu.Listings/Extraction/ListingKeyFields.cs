using System.Text.Json.Nodes;

namespace Latrovu.Listings.Extraction;

public static class ListingKeyFields
{
    public const int MaxQuoteLength = 100;

    public static readonly IReadOnlyList<string> Paths =
    [
        "title",
        "offerType",
        "objectType",
        "location.street",
        "location.houseNumber",
        "location.zip",
        "location.city",
        "size.rooms",
        "size.livingAreaM2",
        "building.floor",
        "building.yearBuilt",
        "rent.netChf",
        "rent.chargesChf",
        "rent.grossChf",
        "sale.priceChf",
        "sale.ownershipType",
        "availability.availableFrom",
        "contact.name",
        "contact.email",
        "contact.phone"
    ];

    public static readonly IReadOnlySet<string> InferredPaths = new HashSet<string>
    {
        "offerType",
        "objectType",
        "sale.ownershipType"
    };

    public static string AsBulletList() => string.Join(Environment.NewLine, Paths.Select(path => $"  - {path}"));

    public static string EvidenceSchemaJson()
    {
        JsonObject properties = [];
        foreach (string path in Paths)
        {
            properties[path] = new JsonObject
            {
                ["type"] = new JsonArray("string", "null"),
                ["maxLength"] = MaxQuoteLength
            };
        }

        JsonObject schema = new()
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["additionalProperties"] = false
        };
        return schema.ToJsonString();
    }
}
