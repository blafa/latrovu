namespace Latrovu.Listings.Ingestion;

public sealed record NewProject(
    string SourceEmail,
    string? FileName,
    string? SourceHash,
    string Title,
    int UnitCount,
    int RentedCount);
