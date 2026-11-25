using Latrovu.Listings.Renewal;
using Microsoft.Extensions.Logging.Abstractions;

namespace Latrovu.Listings.Tests.Renewal;

public sealed class RenewalReminderServiceTests
{
    private const string AgentAddress = "agent@example.com";
    private const string Title = "3½-Zimmer-Wohnung";

    private static readonly Guid ListingId = Guid.NewGuid();
    private static readonly DateTimeOffset Start = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider time = new(Start);
    private readonly FakeListingRenewalStore renewals = new();
    private readonly FakeListingReviewStore reviews = new();
    private readonly FakeRenewalNotifier notifier = new();
    private readonly RenewalReminderService service;

    public RenewalReminderServiceTests()
    {
        ListingRemover remover = new(reviews, new FakeObjectStorage(), NullLogger<ListingRemover>.Instance);
        service = new RenewalReminderService(
            renewals,
            new FakeListingAccessTokenStore("review-token", ListingId),
            notifier,
            remover,
            time,
            NullLogger<RenewalReminderService>.Instance);
    }

    [Fact]
    public async Task Run_BeforeOneMonth_SendsNothing()
    {
        renewals.Add(State(Start, remindersSent: 0, lastReminderAt: null));
        time.Advance(RenewalReminderService.ReminderInterval - TimeSpan.FromHours(1));

        RenewalRun run = await service.RunAsync(CancellationToken.None);

        Assert.Equal(0, run.RemindersSent);
        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public async Task Run_AfterOneMonth_SendsTheFirstReminderOnlyOnce()
    {
        renewals.Add(State(Start, remindersSent: 0, lastReminderAt: null));
        time.Advance(RenewalReminderService.ReminderInterval);

        await service.RunAsync(CancellationToken.None);
        await service.RunAsync(CancellationToken.None);

        RenewalReminder reminder = Assert.Single(notifier.Sent);
        Assert.Equal(1, reminder.Number);
        Assert.Null(reminder.DeletionAt);
        Assert.Equal(1, renewals[ListingId].RemindersSent);
    }

    [Fact]
    public async Task Run_ThirdReminder_AnnouncesDeletionOneDayLater()
    {
        renewals.Add(State(Start, remindersSent: 2, lastReminderAt: Start + (RenewalReminderService.ReminderInterval * 2)));
        time.Advance(RenewalReminderService.ReminderInterval * RenewalReminderService.MaxReminders);

        await service.RunAsync(CancellationToken.None);

        RenewalReminder reminder = Assert.Single(notifier.Sent);
        Assert.Equal(RenewalReminderService.MaxReminders, reminder.Number);
        Assert.Equal(time.GetUtcNow() + RenewalReminderService.GracePeriod, reminder.DeletionAt);
    }

    [Fact]
    public async Task Run_OneDayAfterThirdReminder_RemovesTheListing()
    {
        await reviews.ApproveAsync(ListingId, CancellationToken.None);
        renewals.Add(State(Start, RenewalReminderService.MaxReminders, lastReminderAt: Start));
        time.Advance(RenewalReminderService.GracePeriod);

        RenewalRun run = await service.RunAsync(CancellationToken.None);

        Assert.Equal(1, run.ListingsRemoved);
        Assert.Equal(ListingReview.WithdrawnStatus, reviews.Status);
        Assert.Empty(notifier.Sent);
    }

    [Fact]
    public async Task Run_WithinGracePeriod_KeepsTheListing()
    {
        await reviews.ApproveAsync(ListingId, CancellationToken.None);
        renewals.Add(State(Start, RenewalReminderService.MaxReminders, lastReminderAt: Start));
        time.Advance(RenewalReminderService.GracePeriod - TimeSpan.FromMinutes(1));

        RenewalRun run = await service.RunAsync(CancellationToken.None);

        Assert.Equal(0, run.ListingsRemoved);
        Assert.Equal(ListingReview.ApprovedStatus, reviews.Status);
    }

    private static RenewalState State(DateTimeOffset renewedAt, int remindersSent, DateTimeOffset? lastReminderAt) =>
        new(ListingId, AgentAddress, Title, renewedAt, remindersSent, lastReminderAt);
}
