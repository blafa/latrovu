namespace Latrovu.Listings.Review;

public sealed record ListingReview(
    Guid Id,
    string Status,
    string SourceEmail,
    string? FileName,
    string DataJson,
    string FieldScoresJson,
    string FieldEvidenceJson,
    bool PublishContact,
    IReadOnlyList<ListingImageRef> Images);
