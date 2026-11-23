using System.Text.Json;
using Latrovu.BuildingBlocks.Ai;

namespace Latrovu.Listings;

public static class DocumentClassificationParser
{
    private const string DocumentTypeProperty = "documentType";
    private const string ReasonProperty = "reason";
    private const string UnparsableReason = "Classification answer could not be read";

    private static readonly Dictionary<string, DocumentType> TypesByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["single_listing"] = DocumentType.SingleListing,
        ["multi_unit_listing"] = DocumentType.MultiUnitListing,
        ["application_form"] = DocumentType.ApplicationForm,
        ["contract"] = DocumentType.Contract,
        ["other"] = DocumentType.Other
    };

    public static DocumentClassification? Parse(string? answer)
    {
        string? json = LlmJsonNormalizer.Normalize(answer);
        if (json is null)
        {
            return null;
        }

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty(DocumentTypeProperty, out JsonElement typeElement)
            || !TypesByName.TryGetValue(typeElement.GetString() ?? string.Empty, out DocumentType type))
        {
            return null;
        }

        return new DocumentClassification(type, ReadReason(root));
    }

    public static DocumentClassification FallbackToListing() => new(DocumentType.SingleListing, UnparsableReason);

    private static string? ReadReason(JsonElement root) =>
        root.TryGetProperty(ReasonProperty, out JsonElement reason) && reason.ValueKind == JsonValueKind.String
            ? reason.GetString()
            : null;
}
