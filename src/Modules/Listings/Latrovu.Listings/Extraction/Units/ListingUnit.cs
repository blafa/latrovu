namespace Latrovu.Listings.Extraction.Units;

public sealed record ListingUnit(
    string Label,
    decimal? Rooms,
    int? Floor,
    decimal? LivingAreaM2,
    decimal? NetRentChf,
    decimal? ChargesChf,
    decimal? GrossRentChf,
    decimal? PriceChf,
    string? AvailableFrom,
    UnitStatus Status);
