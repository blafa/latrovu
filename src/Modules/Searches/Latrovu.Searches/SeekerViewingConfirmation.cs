namespace Latrovu.Searches;

public sealed record SeekerViewingConfirmation(
    string Address,
    string Channel,
    string Language,
    string ListingTitle,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);
