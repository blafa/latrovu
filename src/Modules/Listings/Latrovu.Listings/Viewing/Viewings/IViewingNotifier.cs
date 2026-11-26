namespace Latrovu.Listings.Viewing.Viewings;

public interface IViewingNotifier
{
    Task NotifyAgentOfBookingAsync(Guid listingId, ViewingSlot slot, string tokenHash, CancellationToken ct);

    Task NotifyAgentOfRescheduleAsync(Guid listingId, ViewingSlot slot, ViewingSlot previous, string tokenHash, CancellationToken ct);

    Task NotifyAgentOfCancellationAsync(Guid listingId, ViewingSlot slot, string tokenHash, CancellationToken ct);
}
