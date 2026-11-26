namespace Latrovu.Core.Listings;

public sealed record ViewingBooked(
    Guid SearchId,
    Guid ListingId,
    string ListingTitle,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset? PreviousStartsAt = null,
    DateTimeOffset? PreviousEndsAt = null);
