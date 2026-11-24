using Latrovu.BuildingBlocks.Ai;

namespace Latrovu.Searches.Infrastructure;

public static class SearchInterpretationSchema
{
    public static readonly JsonResponseSchema Value = new("search_interpretation", JsonSchemaSealer.Seal(Json));

    private const string Json = """
        {
          "type": "object",
          "properties": {
            "language": { "type": "string" },
            "action": { "enum": ["new", "update", "pause", "resume", "stop", "unclear"] },
            "searchName": { "type": ["string", "null"] },
            "criteria": {
              "type": "object",
              "properties": {
                "offerType": { "enum": ["rent", "sale", null] },
                "places": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "name": { "type": "string" },
                      "within": { "type": ["string", "null"] },
                      "kind": { "enum": ["canton", "region", "lake", null] },
                      "nearKm": { "type": ["number", "null"] }
                    },
                    "required": ["name", "within", "kind", "nearKm"]
                  }
                },
                "roomsMin": { "type": ["number", "null"] },
                "roomsMax": { "type": ["number", "null"] },
                "priceMaxChf": { "type": ["number", "null"] },
                "livingAreaMinM2": { "type": ["number", "null"] },
                "availableFrom": { "type": ["string", "null"] },
                "mustHaves": { "type": "array", "items": { "type": "string" } },
                "notes": { "type": ["string", "null"] }
              },
              "required": ["offerType", "places", "roomsMin", "roomsMax", "priceMaxChf", "livingAreaMinM2", "availableFrom", "mustHaves", "notes"]
            },
            "summary": { "type": "string" },
            "refinements": { "type": "array", "items": { "type": "string" } }
          },
          "required": ["language", "action", "searchName", "criteria", "summary", "refinements"]
        }
        """;
}
