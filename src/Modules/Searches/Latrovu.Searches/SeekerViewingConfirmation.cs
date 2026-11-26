namespace Latrovu.Searches;

public sealed record SeekerViewingConfirmation(
    string Address,
    string Channel,
    string Language,
    string ListingTitle,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsCancellation = false,
    DateTimeOffset? PreviousStartsAt = null,
    DateTimeOffset? PreviousEndsAt = null)
{
    public bool IsReschedule => !IsCancellation && PreviousStartsAt is not null && PreviousEndsAt is not null;
}
