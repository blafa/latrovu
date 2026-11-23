using Latrovu.BuildingBlocks.Ai;

namespace Latrovu.Listings;

public static class DocumentClassificationSchema
{
    public static readonly JsonResponseSchema Value = new("document_classification", Json);

    private const string Json = """
        {
          "type": "object",
          "properties": {
            "documentType": { "enum": ["single_listing", "multi_unit_listing", "application_form", "contract", "other"] },
            "reason": { "type": "string" }
          },
          "required": ["documentType", "reason"]
        }
        """;
}
