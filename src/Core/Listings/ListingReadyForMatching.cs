namespace Latrovu.Core.Listings;

public sealed record ListingReadyForMatching(
    Guid ListingId,
    double East,
    double North,
    string LocationPrecision,
    string? OfferType,
    decimal? Rooms,
    decimal? PriceChf,
    decimal? LivingAreaM2,
    string? AvailableFrom);
