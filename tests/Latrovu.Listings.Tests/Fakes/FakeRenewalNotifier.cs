using Latrovu.Listings.Renewal;

namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeRenewalNotifier : IRenewalNotifier
{
    public List<RenewalReminder> Sent { get; } = [];

    public Task SendReminderAsync(RenewalReminder reminder, CancellationToken ct)
    {
        Sent.Add(reminder);
        return Task.CompletedTask;
    }
}
