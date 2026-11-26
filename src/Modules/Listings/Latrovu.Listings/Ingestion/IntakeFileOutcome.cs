namespace Latrovu.Listings.Ingestion;

public sealed record IntakeFileOutcome(
    string FileName,
    IntakeFileStatus Status,
    string? ReviewToken = null,
    DocumentType? DocumentType = null,
    Guid? ListingId = null,
    IntakeProjectOutcome? Project = null);
