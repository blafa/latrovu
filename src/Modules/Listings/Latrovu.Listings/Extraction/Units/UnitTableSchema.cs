using Latrovu.BuildingBlocks.Ai;

namespace Latrovu.Listings.Extraction.Units;

public static class UnitTableSchema
{
    public static readonly JsonResponseSchema Value = new("units", JsonSchemaSealer.Seal(Json));

    private const string Json = """
        {
          "type": "object",
          "properties": {
            "units": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "label": { "type": "string" },
                  "rooms": { "type": ["number", "null"] },
                  "floor": { "type": ["number", "null"] },
                  "livingAreaM2": { "type": ["number", "null"] },
                  "netRentChf": { "type": ["number", "null"] },
                  "chargesChf": { "type": ["number", "null"] },
                  "grossRentChf": { "type": ["number", "null"] },
                  "priceChf": { "type": ["number", "null"] },
                  "availableFrom": { "type": ["string", "null"] },
                  "status": { "enum": ["available", "reserved", "rented", null] }
                },
                "required": ["label", "rooms", "floor", "livingAreaM2", "netRentChf", "chargesChf", "grossRentChf", "priceChf", "availableFrom", "status"]
              }
            }
          },
          "required": ["units"]
        }
        """;
}
