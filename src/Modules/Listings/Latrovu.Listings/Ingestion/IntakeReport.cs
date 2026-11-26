namespace Latrovu.Listings.Ingestion;

public sealed record IntakeReport(string Recipient, string? Subject, IReadOnlyList<IntakeFileOutcome> Files)
{
    public bool HasNoPdf => Files.Count == 0;

    public bool HasListings => Files.Any(file => file.Status != IntakeFileStatus.NotAListing);
}
