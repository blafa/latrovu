namespace Latrovu.Core.Listings;

public sealed record ViewingBooked(
    Guid SearchId,
    Guid ListingId,
    string ListingTitle,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);
