namespace Latrovu.Listings.Viewing.Viewings;

public interface IViewingNotifier
{
    Task NotifyAgentOfBookingAsync(Guid listingId, ViewingSlot slot, string tokenHash, CancellationToken ct);
}
