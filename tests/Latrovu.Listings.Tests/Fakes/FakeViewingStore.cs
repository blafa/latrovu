using Latrovu.Listings.Viewing.Viewings;

namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeViewingStore : IViewingStore, IViewingNotifier
{
    private readonly List<(long Id, Guid ListingId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int Capacity)> slots = [];
    private readonly List<(long SlotId, string TokenHash)> bookings = [];

    public List<long> NotifiedSlots { get; } = [];

    public List<long> CancelledSlots { get; } = [];

    public List<(long SlotId, long PreviousSlotId)> RescheduledSlots { get; } = [];

    public Task<IReadOnlyList<ViewingSlot>> GetSlotsAsync(Guid listingId, string? tokenHash, DateTimeOffset from, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ViewingSlot>>(slots
            .Where(slot => slot.ListingId == listingId && slot.EndsAt > from)
            .OrderBy(slot => slot.StartsAt)
            .Select(slot => new ViewingSlot(
                slot.Id,
                slot.StartsAt,
                slot.EndsAt,
                slot.Capacity,
                bookings.Count(booking => booking.SlotId == slot.Id),
                bookings.Contains((slot.Id, tokenHash ?? string.Empty)),
                []))
            .ToList());

    public Task<long> AddSlotAsync(Guid listingId, DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity, CancellationToken ct)
    {
        long id = slots.Count + 1;
        slots.Add((id, listingId, startsAt, endsAt, capacity));
        return Task.FromResult(id);
    }

    public Task<bool> RemoveEmptySlotAsync(Guid listingId, long slotId, CancellationToken ct) =>
        Task.FromResult(!bookings.Any(booking => booking.SlotId == slotId)
            && slots.RemoveAll(slot => slot.Id == slotId && slot.ListingId == listingId) > 0);

    public Task<BookingOutcome> BookAsync(Guid listingId, long slotId, string tokenHash, DateTimeOffset now, CancellationToken ct)
    {
        int index = slots.FindIndex(slot => slot.Id == slotId && slot.ListingId == listingId && slot.EndsAt > now);
        if (index < 0)
        {
            return Task.FromResult(BookingOutcome.NotFound);
        }

        if (bookings.Contains((slotId, tokenHash)))
        {
            return Task.FromResult(BookingOutcome.Booked);
        }

        if (bookings.Count(booking => booking.SlotId == slotId) >= slots[index].Capacity)
        {
            return Task.FromResult(BookingOutcome.Full);
        }

        bookings.RemoveAll(booking => booking.TokenHash == tokenHash);
        bookings.Add((slotId, tokenHash));
        return Task.FromResult(BookingOutcome.Booked);
    }

    public Task<bool> CancelAsync(Guid listingId, string tokenHash, CancellationToken ct) =>
        Task.FromResult(bookings.RemoveAll(booking => booking.TokenHash == tokenHash) > 0);

    public Task NotifyAgentOfBookingAsync(Guid listingId, ViewingSlot slot, string tokenHash, CancellationToken ct)
    {
        NotifiedSlots.Add(slot.Id);
        return Task.CompletedTask;
    }

    public Task NotifyAgentOfRescheduleAsync(Guid listingId, ViewingSlot slot, ViewingSlot previous, string tokenHash, CancellationToken ct)
    {
        RescheduledSlots.Add((slot.Id, previous.Id));
        return Task.CompletedTask;
    }

    public Task NotifyAgentOfCancellationAsync(Guid listingId, ViewingSlot slot, string tokenHash, CancellationToken ct)
    {
        CancelledSlots.Add(slot.Id);
        return Task.CompletedTask;
    }
}
