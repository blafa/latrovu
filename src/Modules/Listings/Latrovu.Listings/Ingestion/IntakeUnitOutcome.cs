namespace Latrovu.Listings.Ingestion;

public sealed record IntakeUnitOutcome(string Label, decimal? Rooms, decimal? GrossRentChf, string ReviewToken, Guid ListingId);
