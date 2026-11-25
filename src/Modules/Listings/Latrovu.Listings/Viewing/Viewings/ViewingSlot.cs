namespace Latrovu.Listings.Viewing.Viewings;

public sealed record ViewingSlot(
    long Id,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int Capacity,
    int Booked,
    bool BookedByMe,
    IReadOnlyList<string> Attendees)
{
    public int FreePlaces => Math.Max(0, Capacity - Booked);
}
