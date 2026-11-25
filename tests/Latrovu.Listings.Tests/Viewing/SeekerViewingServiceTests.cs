using Latrovu.Listings.Viewing.Chat;
using Latrovu.Listings.Viewing.Viewings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Latrovu.Listings.Tests.Viewing;

public sealed class SeekerViewingServiceTests
{
    private static readonly Guid ListingId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider time = new(Now);
    private readonly FakeListingReviewStore listings = new();
    private readonly FakeListingViewLinkStore links = new();
    private readonly FakeListingChatStore chats = new();
    private readonly FakeViewingStore viewings = new();
    private readonly ListingViewService views;
    private readonly SeekerViewingService service;

    public SeekerViewingServiceTests()
    {
        views = new ListingViewService(links, listings, time, NullLogger<ListingViewService>.Instance);
        service = new SeekerViewingService(views, viewings, chats, viewings, time, NullLogger<SeekerViewingService>.Instance);
    }

    [Fact]
    public async Task Book_FreeSlot_ConfirmsInChatAndNotifiesAgent()
    {
        string token = await ApprovedLinkAsync();
        long slotId = await SlotAsync(capacity: 2);

        ViewingResult result = await service.BookAsync(token, slotId, CancellationToken.None);

        Assert.Equal(BookingOutcome.Booked, result.Outcome);
        Assert.True(result.Slots.Single().BookedByMe);
        Assert.Equal([slotId], viewings.NotifiedSlots);
        Assert.Single(await chats.GetMessagesAsync(token, CancellationToken.None));
    }

    [Fact]
    public async Task Book_FullSlot_ReturnsFull()
    {
        string first = await ApprovedLinkAsync();
        string second = await LinkAsync();
        long slotId = await SlotAsync(capacity: 1);
        await service.BookAsync(first, slotId, CancellationToken.None);

        ViewingResult result = await service.BookAsync(second, slotId, CancellationToken.None);

        Assert.Equal(BookingOutcome.Full, result.Outcome);
        Assert.False(result.Slots.Single().BookedByMe);
    }

    [Fact]
    public async Task Book_OtherSlot_MovesTheBooking()
    {
        string token = await ApprovedLinkAsync();
        long morning = await SlotAsync(capacity: 3);
        long evening = await SlotAsync(capacity: 3, hoursAhead: 30);
        await service.BookAsync(token, morning, CancellationToken.None);

        ViewingResult result = await service.BookAsync(token, evening, CancellationToken.None);

        Assert.Equal(0, result.Slots.Single(slot => slot.Id == morning).Booked);
        Assert.True(result.Slots.Single(slot => slot.Id == evening).BookedByMe);
    }

    [Fact]
    public async Task Cancel_RemovesTheBooking()
    {
        string token = await ApprovedLinkAsync();
        long slotId = await SlotAsync(capacity: 2);
        await service.BookAsync(token, slotId, CancellationToken.None);

        ViewingResult result = await service.CancelAsync(token, CancellationToken.None);

        Assert.Equal(0, result.Slots.Single().Booked);
    }

    [Fact]
    public async Task Book_UnknownLink_IsNotFound()
    {
        ViewingResult result = await service.BookAsync("unknown", 1, CancellationToken.None);

        Assert.False(result.Found);
    }

    [Fact]
    public async Task Book_SharedLink_IsRefused()
    {
        string shared = (await views.ShareAsync(await ApprovedLinkAsync(), CancellationToken.None))!;
        long slotId = await SlotAsync(capacity: 2);

        ViewingResult result = await service.BookAsync(shared, slotId, CancellationToken.None);

        Assert.False(result.Found);
        Assert.Empty(viewings.NotifiedSlots);
    }

    private async Task<long> SlotAsync(int capacity, int hoursAhead = 24) =>
        await viewings.AddSlotAsync(ListingId, Now.AddHours(hoursAhead), Now.AddHours(hoursAhead).AddMinutes(30), capacity, CancellationToken.None);

    private async Task<string> ApprovedLinkAsync()
    {
        await listings.ApproveAsync(ListingId, CancellationToken.None);
        return await LinkAsync();
    }

    private async Task<string> LinkAsync() =>
        (await views.CreateLinkAsync(ListingId, searchId: null, CancellationToken.None))!;
}
