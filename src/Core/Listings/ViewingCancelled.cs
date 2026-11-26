namespace Latrovu.Core.Listings;

public sealed record ViewingCancelled(
    Guid SearchId,
    Guid ListingId,
    string ListingTitle,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);
