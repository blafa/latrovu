namespace Latrovu.Listings.Renewal;

public interface IRenewalNotifier
{
    Task SendReminderAsync(RenewalReminder reminder, CancellationToken ct);
}
