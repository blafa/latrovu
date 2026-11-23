using System.Text.Json;
using System.Text.Json.Nodes;

namespace Latrovu.Listings.Infrastructure.Review.Web;

internal sealed class ReviewModel
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private ReviewModel(string token, ListingReview review)
    {
        Token = token;
        Review = review;
        Data = JsonNode.Parse(review.DataJson) as JsonObject ?? [];
        Scores = JsonNode.Parse(review.FieldScoresJson) as JsonObject ?? [];
        Evidence = JsonSerializer.Deserialize<Dictionary<string, EvidenceLocation>>(review.FieldEvidenceJson, WebJson) ?? [];
        Filled = ListingFieldCatalog.Fields.Where(field => ReviewFieldRenderer.HasValue(ValueOf(field))).ToList();
        Empty = ListingFieldCatalog.Fields.Except(Filled).ToList();
        Open = Filled.Where(field => ScoreOf(field) is ReviewFieldRenderer.LowScore or ReviewFieldRenderer.MediumScore).ToList();
    }

    public string Token { get; }

    public ListingReview Review { get; }

    public JsonObject Data { get; }

    public JsonObject Scores { get; }

    public IReadOnlyDictionary<string, EvidenceLocation> Evidence { get; }

    public IReadOnlyList<FieldDefinition> Filled { get; }

    public IReadOnlyList<FieldDefinition> Empty { get; }

    public IReadOnlyList<FieldDefinition> Open { get; }

    public static ReviewModel From(string token, ListingReview review)
    {
        ArgumentNullException.ThrowIfNull(review);
        return new ReviewModel(token, review);
    }

    public JsonNode? ValueOf(FieldDefinition field) => JsonPaths.Get(Data, field.Path);

    public string ScoreOf(FieldDefinition field) => ListingValues.ScoreOf(Scores, field.Path);

    public EvidenceLocation? EvidenceOf(FieldDefinition field) => Evidence.GetValueOrDefault(field.Path);

    public bool IsConfirmed(FieldDefinition field) => ScoreOf(field) == ListingReviewService.ConfirmedScore;
}
