namespace Latrovu.Listings.Viewing.Viewings;

public sealed record ViewingResult(BookingOutcome? Outcome, IReadOnlyList<ViewingSlot> Slots, bool Found = true)
{
    public static ViewingResult Missing { get; } = new(BookingOutcome.NotFound, [], false);
}
