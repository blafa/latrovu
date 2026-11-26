namespace Latrovu.Listings.Ingestion;

public sealed record IntakeProjectOutcome(string Title, int RentedCount, IReadOnlyList<IntakeUnitOutcome> Units);
