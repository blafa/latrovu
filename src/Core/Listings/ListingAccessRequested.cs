namespace Latrovu.Core.Listings;

public sealed record ListingAccessRequested(
    Guid ListingId,
    string Address,
    string Language,
    string ListingTitle,
    string? City);
