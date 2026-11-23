using Latrovu.Api.Ai;

namespace Latrovu.Api.Listings;

public static class ListingSchema
{
    public static readonly JsonResponseSchema Value = new("listing", Json);

    private const string Json = """
        {
          "type": "object",
          "properties": {
            "offerType": { "enum": ["rent", "sale", null] },
            "objectType": { "enum": ["apartment", "studio", "attic", "maisonette", "loft", "terrace_apartment", "furnished_apartment", "room", "house", "parking", "commercial", "plot", "other", null] },
            "title": { "type": ["string", "null"] },
            "referenceNumber": { "type": ["string", "null"] },
            "location": {
              "type": ["object", "null"],
              "properties": {
                "street": { "type": ["string", "null"] },
                "houseNumber": { "type": ["string", "null"] },
                "zip": { "type": ["string", "null"] },
                "city": { "type": ["string", "null"] },
                "municipality": { "type": ["string", "null"] },
                "canton": { "type": ["string", "null"] },
                "district": { "type": ["string", "null"] },
                "distances": {
                  "type": ["array", "null"],
                  "items": {
                    "type": "object",
                    "properties": {
                      "target": { "type": ["string", "null"] },
                      "meters": { "type": ["number", "null"] },
                      "minutes": { "type": ["number", "null"] },
                      "mode": { "enum": ["walk", "transit", "car", "bike", null] }
                    }
                  }
                }
              }
            },
            "size": {
              "type": ["object", "null"],
              "properties": {
                "rooms": { "type": ["number", "null"] },
                "bedrooms": { "type": ["number", "null"] },
                "bathrooms": { "type": ["number", "null"] },
                "toilets": { "type": ["number", "null"] },
                "livingAreaM2": { "type": ["number", "null"] },
                "grossFloorAreaM2": { "type": ["number", "null"] },
                "usableAreaM2": { "type": ["number", "null"] },
                "plotAreaM2": { "type": ["number", "null"] },
                "balconyAreaM2": { "type": ["number", "null"] },
                "gardenAreaM2": { "type": ["number", "null"] },
                "ceilingHeightM": { "type": ["number", "null"] }
              }
            },
            "building": {
              "type": ["object", "null"],
              "properties": {
                "floor": { "type": ["number", "null"] },
                "floorsInBuilding": { "type": ["number", "null"] },
                "unitsInBuilding": { "type": ["number", "null"] },
                "yearBuilt": { "type": ["number", "null"] },
                "lastRenovationYear": { "type": ["number", "null"] },
                "condition": { "enum": ["new", "first_occupancy", "renovated", "good", "needs_renovation", null] }
              }
            },
            "rent": {
              "type": ["object", "null"],
              "properties": {
                "netChf": { "type": ["number", "null"] },
                "chargesChf": { "type": ["number", "null"] },
                "chargesType": { "enum": ["akonto", "pauschal", null] },
                "grossChf": { "type": ["number", "null"] },
                "depositChf": { "type": ["number", "null"] },
                "depositMonths": { "type": ["number", "null"] },
                "minimumRentalMonths": { "type": ["number", "null"] },
                "rentType": { "enum": ["unlimited", "limited", null] },
                "limitedUntil": { "type": ["string", "null"] }
              }
            },
            "sale": {
              "type": ["object", "null"],
              "properties": {
                "priceChf": { "type": ["number", "null"] },
                "pricePerM2Chf": { "type": ["number", "null"] },
                "ownershipType": { "enum": ["freehold", "condominium", "co_op", null] },
                "valueQuota": { "type": ["string", "null"] },
                "condoFeesChfPerYear": { "type": ["number", "null"] },
                "renovationFundChf": { "type": ["number", "null"] },
                "handoverCosts": { "type": ["string", "null"] },
                "landRegistryNumber": { "type": ["string", "null"] }
              }
            },
            "parking": {
              "type": ["array", "null"],
              "items": {
                "type": "object",
                "properties": {
                  "type": { "enum": ["garage", "underground", "carport", "outdoor", null] },
                  "count": { "type": ["number", "null"] },
                  "priceChf": { "type": ["number", "null"] },
                  "pricePeriod": { "enum": ["month", "once", null] },
                  "included": { "type": ["boolean", "null"] }
                }
              }
            },
            "features": {
              "type": ["object", "null"],
              "properties": {
                "balcony": { "type": ["boolean", "null"] },
                "terrace": { "type": ["boolean", "null"] },
                "loggia": { "type": ["boolean", "null"] },
                "garden": { "type": ["boolean", "null"] },
                "elevator": { "type": ["boolean", "null"] },
                "cellar": { "type": ["boolean", "null"] },
                "atticStorage": { "type": ["boolean", "null"] },
                "storageRoom": { "type": ["boolean", "null"] },
                "ownWashingMachine": { "type": ["boolean", "null"] },
                "sharedLaundry": { "type": ["boolean", "null"] },
                "dishwasher": { "type": ["boolean", "null"] },
                "fireplace": { "type": ["boolean", "null"] },
                "floorHeating": { "type": ["boolean", "null"] },
                "wheelchairAccessible": { "type": ["boolean", "null"] },
                "stepFree": { "type": ["boolean", "null"] },
                "petsAllowed": { "type": ["boolean", "null"] },
                "smokingAllowed": { "type": ["boolean", "null"] },
                "furnished": { "type": ["boolean", "null"] },
                "view": { "type": ["string", "null"] },
                "orientation": { "type": ["string", "null"] },
                "kitchen": { "type": ["string", "null"] },
                "bathroomDetails": { "type": ["string", "null"] },
                "flooring": { "type": ["array", "null"], "items": { "type": "string" } },
                "other": { "type": ["array", "null"], "items": { "type": "string" } }
              }
            },
            "energy": {
              "type": ["object", "null"],
              "properties": {
                "heatingType": { "enum": ["heat_pump", "district", "gas", "oil", "wood", "pellets", "electric", "solar", "other", null] },
                "heatDistribution": { "enum": ["floor", "radiators", "other", null] },
                "geakClass": { "type": ["string", "null"] },
                "minergie": { "type": ["string", "null"] },
                "solarPanels": { "type": ["boolean", "null"] },
                "evCharging": { "type": ["boolean", "null"] }
              }
            },
            "availability": {
              "type": ["object", "null"],
              "properties": {
                "availableFrom": { "type": ["string", "null"] }
              }
            },
            "viewing": {
              "type": ["object", "null"],
              "properties": {
                "info": { "type": ["string", "null"] },
                "dates": { "type": ["array", "null"], "items": { "type": "string" } }
              }
            },
            "application": {
              "type": ["object", "null"],
              "properties": {
                "instructions": { "type": ["string", "null"] },
                "requiredDocuments": { "type": ["array", "null"], "items": { "type": "string" } },
                "applicationUrl": { "type": ["string", "null"] }
              }
            },
            "contact": {
              "type": ["object", "null"],
              "properties": {
                "company": { "type": ["string", "null"] },
                "name": { "type": ["string", "null"] },
                "role": { "type": ["string", "null"] },
                "email": { "type": ["string", "null"] },
                "phone": { "type": ["string", "null"] },
                "website": { "type": ["string", "null"] }
              }
            },
            "description": {
              "type": ["object", "null"],
              "properties": {
                "summary": { "type": ["string", "null"] },
                "original": { "type": ["string", "null"] }
              }
            },
            "documents": {
              "type": ["object", "null"],
              "properties": {
                "hasFloorPlan": { "type": ["boolean", "null"] },
                "mentionedAttachments": { "type": ["array", "null"], "items": { "type": "string" } }
              }
            },
            "otherDetails": {
              "type": ["array", "null"],
              "items": {
                "type": "object",
                "properties": {
                  "label": { "type": "string" },
                  "value": { "type": "string" }
                },
                "required": ["label", "value"]
              }
            }
          },
          "required": ["offerType", "objectType", "title", "location", "size", "contact", "description"]
        }
        """;
}
