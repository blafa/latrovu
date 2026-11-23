using System.Text.Json;
using System.Text.Json.Nodes;

namespace Latrovu.Listings.Review;

public sealed class ListingReviewService(
    IListingAccessTokenStore tokens,
    IListingReviewStore reviews,
    ILogger<ListingReviewService> logger) : IListingReviewService
{
    public const string ConfirmedScore = "confirmed";

    public async Task<ListingReview?> GetByTokenAsync(string token, CancellationToken ct)
    {
        Guid? listingId = await tokens.FindListingIdAsync(token, ct);
        return listingId is null ? null : await reviews.GetAsync(listingId.Value, ct);
    }

    public async Task<FieldUpdateResult> UpdateFieldAsync(string token, string path, string? rawValue, CancellationToken ct)
    {
        FieldDefinition? field = ListingFieldCatalog.Find(path);
        if (field is null)
        {
            return FieldUpdateResult.UnknownField;
        }

        if (!TryParse(field, rawValue, out JsonNode? value))
        {
            return FieldUpdateResult.InvalidValue;
        }

        ListingReview? review = await GetByTokenAsync(token, ct);
        if (review is null)
        {
            return FieldUpdateResult.NotFound;
        }

        await SaveFieldAsync(review, path, value, ct);
        logger.LogInformation("Listing {ListingId}: field {Path} updated by reviewer", review.Id, path);
        return FieldUpdateResult.Saved;
    }

    public async Task<bool> ApproveAsync(string token, CancellationToken ct)
    {
        Guid? listingId = await tokens.FindListingIdAsync(token, ct);
        bool approved = listingId is not null && await reviews.ApproveAsync(listingId.Value, ct);
        logger.LogInformation("Approval for listing {ListingId}: {Approved}", listingId, approved);
        return approved;
    }

    private async Task SaveFieldAsync(ListingReview review, string path, JsonNode? value, CancellationToken ct)
    {
        JsonObject data = JsonNode.Parse(review.DataJson) as JsonObject ?? [];
        JsonObject scores = JsonNode.Parse(review.FieldScoresJson) as JsonObject ?? [];
        JsonPaths.Set(data, path, value);
        scores[path] = ConfirmedScore;
        await reviews.SaveAsync(review.Id, data.ToJsonString(), scores.ToJsonString(), ct);
    }

    private static bool TryParse(FieldDefinition field, string? rawValue, out JsonNode? value)
    {
        try
        {
            value = FieldValueParser.Parse(field, rawValue);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            value = null;
            return false;
        }
    }
}
