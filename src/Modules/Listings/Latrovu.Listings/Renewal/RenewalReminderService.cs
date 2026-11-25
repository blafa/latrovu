namespace Latrovu.Listings.Renewal;

public sealed class RenewalReminderService(
    IListingRenewalStore renewals,
    IListingAccessTokenStore accessTokens,
    IRenewalNotifier notifier,
    ListingRemover remover,
    TimeProvider timeProvider,
    ILogger<RenewalReminderService> logger)
{
    public const int MaxReminders = 3;
    public static readonly TimeSpan ReminderInterval = TimeSpan.FromDays(30);
    public static readonly TimeSpan GracePeriod = TimeSpan.FromDays(1);

    public async Task<RenewalRun> RunAsync(CancellationToken ct)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        int reminded = 0;
        int removed = 0;
        foreach (RenewalState state in await renewals.GetApprovedAsync(ct))
        {
            if (IsExpired(state, now))
            {
                removed += await remover.RemoveAsync(state.ListingId, ct) ? 1 : 0;
            }
            else if (IsReminderDue(state, now))
            {
                await RemindAsync(state, now, ct);
                reminded++;
            }
        }

        logger.LogInformation("Renewal run: {ReminderCount} reminders sent, {RemovedCount} listings removed", reminded, removed);
        return new RenewalRun(reminded, removed);
    }

    private async Task RemindAsync(RenewalState state, DateTimeOffset now, CancellationToken ct)
    {
        int number = state.RemindersSent + 1;
        string renewToken = await renewals.CreateTokenAsync(state.ListingId, now + ReminderInterval + GracePeriod, ct);
        string reviewToken = await accessTokens.CreateAsync(state.ListingId, ct);
        DateTimeOffset? deletionAt = number == MaxReminders ? now + GracePeriod : null;
        await notifier.SendReminderAsync(new RenewalReminder(state.AgentAddress, state.Title, number, renewToken, reviewToken, deletionAt), ct);
        await renewals.MarkReminderSentAsync(state.ListingId, number, now, ct);
        logger.LogInformation("Renewal reminder {Number} sent for listing {ListingId}", number, state.ListingId);
    }

    private static bool IsReminderDue(RenewalState state, DateTimeOffset now) =>
        state.RemindersSent < MaxReminders && now >= state.RenewedAt + ReminderInterval * (state.RemindersSent + 1);

    private static bool IsExpired(RenewalState state, DateTimeOffset now) =>
        state.RemindersSent >= MaxReminders && state.LastReminderAt is { } last && now >= last + GracePeriod;
}
