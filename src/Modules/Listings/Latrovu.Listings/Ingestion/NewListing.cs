namespace Latrovu.Listings.Ingestion;

public sealed record NewListing(
    string SourceEmail,
    string? Subject,
    string? FileName,
    ListingExtraction? Extraction,
    IReadOnlyList<string> SourcePages);
