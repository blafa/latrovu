using Latrovu.Listings.Renewal;
using Microsoft.Extensions.Logging.Abstractions;

namespace Latrovu.Listings.Tests.Renewal;

public sealed class ListingRenewalServiceTests
{
    private static readonly Guid ListingId = Guid.NewGuid();
    private static readonly DateTimeOffset Start = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider time = new(Start);
    private readonly FakeListingRenewalStore renewals = new();
    private readonly ListingRenewalService service;

    public ListingRenewalServiceTests()
    {
        service = new ListingRenewalService(renewals, time, NullLogger<ListingRenewalService>.Instance);
        renewals.Add(new RenewalState(ListingId, "agent@example.com", "Wohnung", Start, 2, Start));
    }

    [Fact]
    public async Task Renew_ValidToken_RestartsTheClock()
    {
        string token = await renewals.CreateTokenAsync(ListingId, Start + TimeSpan.FromDays(31), CancellationToken.None);
        time.Advance(TimeSpan.FromDays(5));

        bool renewed = await service.RenewAsync(token, CancellationToken.None);

        Assert.True(renewed);
        Assert.Equal(0, renewals[ListingId].RemindersSent);
        Assert.Equal(time.GetUtcNow(), renewals[ListingId].RenewedAt);
    }

    [Fact]
    public async Task Renew_ExpiredToken_IsRefused()
    {
        string token = await renewals.CreateTokenAsync(ListingId, Start + TimeSpan.FromDays(1), CancellationToken.None);
        time.Advance(TimeSpan.FromDays(2));

        Assert.False(await service.IsValidAsync(token, CancellationToken.None));
        Assert.False(await service.RenewAsync(token, CancellationToken.None));
        Assert.Equal(2, renewals[ListingId].RemindersSent);
    }
}
