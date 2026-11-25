using Latrovu.Listings.Viewing.Viewings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Latrovu.Listings.Tests.Viewing;

public sealed class AgentViewingServiceTests
{
    private const string Token = FakeListingReviewService.ValidToken;
    private const string BookerHash = "booker-hash";
    private const int SlotMinutes = 30;

    private static readonly Guid ListingId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Tomorrow = Now.AddDays(1);

    private readonly FakeViewingStore viewings = new();
    private readonly AgentViewingService service;

    public AgentViewingServiceTests()
    {
        service = new AgentViewingService(
            new FakeListingReviewService(ListingId),
            viewings,
            new FakeTimeProvider(Now),
            NullLogger<AgentViewingService>.Instance);
    }

    [Fact]
    public async Task Add_ValidSlot_IsListed()
    {
        ViewingResult result = await service.AddAsync(Token, Tomorrow, Tomorrow.AddMinutes(SlotMinutes), 3, CancellationToken.None);

        Assert.Null(result.Outcome);
        Assert.Equal(3, result.Slots.Single().Capacity);
    }

    [Theory]
    [InlineData(-24, SlotMinutes, 3)]
    [InlineData(24, 0, 3)]
    [InlineData(24, SlotMinutes, 0)]
    [InlineData(24, SlotMinutes, AgentViewingService.MaxCapacity + 1)]
    public async Task Add_InvalidSlot_IsRejected(int hoursAhead, int minutes, int capacity)
    {
        DateTimeOffset startsAt = Now.AddHours(hoursAhead);

        ViewingResult result = await service.AddAsync(Token, startsAt, startsAt.AddMinutes(minutes), capacity, CancellationToken.None);

        Assert.Equal(BookingOutcome.NotFound, result.Outcome);
        Assert.Empty(result.Slots);
    }

    [Fact]
    public async Task Remove_EmptySlot_RemovesIt()
    {
        long slotId = await AddSlotAsync();

        ViewingResult result = await service.RemoveAsync(Token, slotId, CancellationToken.None);

        Assert.Empty(result.Slots);
    }

    [Fact]
    public async Task Remove_BookedSlot_KeepsIt()
    {
        long slotId = await AddSlotAsync();
        await viewings.BookAsync(ListingId, slotId, BookerHash, Now, CancellationToken.None);

        ViewingResult result = await service.RemoveAsync(Token, slotId, CancellationToken.None);

        Assert.Equal(1, result.Slots.Single().Booked);
    }

    [Fact]
    public async Task Get_UnknownToken_IsNotFound()
    {
        ViewingResult result = await service.GetAsync("unknown", CancellationToken.None);

        Assert.False(result.Found);
    }

    private async Task<long> AddSlotAsync() =>
        await viewings.AddSlotAsync(ListingId, Tomorrow, Tomorrow.AddMinutes(SlotMinutes), 2, CancellationToken.None);
}
