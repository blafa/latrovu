namespace Latrovu.Listings.Viewing.Viewings;

public interface IViewingStore
{
    Task<IReadOnlyList<ViewingSlot>> GetSlotsAsync(Guid listingId, string? tokenHash, DateTimeOffset from, CancellationToken ct);

    Task<long> AddSlotAsync(Guid listingId, DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity, CancellationToken ct);

    Task<bool> RemoveEmptySlotAsync(Guid listingId, long slotId, CancellationToken ct);

    Task<BookingOutcome> BookAsync(Guid listingId, long slotId, string tokenHash, DateTimeOffset now, CancellationToken ct);

    Task<bool> CancelAsync(Guid listingId, string tokenHash, CancellationToken ct);
}
