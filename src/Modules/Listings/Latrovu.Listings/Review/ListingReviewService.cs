using System.Text.Json;
using System.Text.Json.Nodes;

namespace Latrovu.Listings.Review;

public sealed class ListingReviewService(
    IListingAccessTokenStore tokens,
    IListingReviewStore reviews,
    ILogger<ListingReviewService> logger) : IListingReviewService
{
    public const string ConfirmedScore = "confirmed";
    public const string OtherDetailsPath = "otherDetails";
    private const string LabelKey = "label";
    private const string ValueKey = "value";

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

        return await ChangeAsync(token, path, (data, scores) =>
        {
            JsonPaths.Set(data, path, value);
            scores[path] = ConfirmedScore;
        }, ct);
    }

    public Task<FieldUpdateResult> ConfirmFieldAsync(string token, string path, CancellationToken ct)
    {
        if (ListingFieldCatalog.Find(path) is null)
        {
            return Task.FromResult(FieldUpdateResult.UnknownField);
        }

        return ChangeAsync(token, path, (_, scores) => scores[path] = ConfirmedScore, ct);
    }

    public Task<FieldUpdateResult> AddCustomDetailAsync(string token, string label, string value, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(FieldUpdateResult.InvalidValue);
        }

        return ChangeAsync(token, OtherDetailsPath, (data, _) =>
        {
            JsonArray details = data[OtherDetailsPath] as JsonArray ?? [];
            details.Add(new JsonObject { [LabelKey] = label.Trim(), [ValueKey] = value.Trim() });
            data[OtherDetailsPath] = details;
        }, ct);
    }

    public async Task<bool> SetPublishContactAsync(string token, bool publishContact, CancellationToken ct)
    {
        Guid? listingId = await tokens.FindListingIdAsync(token, ct);
        if (listingId is null)
        {
            return false;
        }

        await reviews.SetPublishContactAsync(listingId.Value, publishContact, ct);
        logger.LogInformation("Listing {ListingId}: contact details public set to {PublishContact}", listingId, publishContact);
        return true;
    }

    public async Task<bool> ApproveAsync(string token, CancellationToken ct)
    {
        Guid? listingId = await tokens.FindListingIdAsync(token, ct);
        bool approved = listingId is not null && await reviews.ApproveAsync(listingId.Value, ct);
        logger.LogInformation("Approval for listing {ListingId}: {Approved}", listingId, approved);
        return approved;
    }

    private async Task<FieldUpdateResult> ChangeAsync(
        string token,
        string path,
        Action<JsonObject, JsonObject> change,
        CancellationToken ct)
    {
        ListingReview? review = await GetByTokenAsync(token, ct);
        if (review is null)
        {
            return FieldUpdateResult.NotFound;
        }

        JsonObject data = JsonNode.Parse(review.DataJson) as JsonObject ?? [];
        JsonObject scores = JsonNode.Parse(review.FieldScoresJson) as JsonObject ?? [];
        change(data, scores);
        await reviews.SaveAsync(review.Id, data.ToJsonString(), scores.ToJsonString(), ct);
        logger.LogInformation("Listing {ListingId}: {Path} changed by reviewer", review.Id, path);
        return FieldUpdateResult.Saved;
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
