using Latrovu.Listings.Viewing.Chat;

namespace Latrovu.Listings.Viewing.Viewings;

public sealed class SeekerViewingService(
    IListingViewService views,
    IViewingStore viewings,
    IListingChatStore chats,
    IViewingNotifier notifier,
    TimeProvider timeProvider,
    ILogger<SeekerViewingService> logger)
{
    public async Task<ViewingResult> GetAsync(string token, CancellationToken ct)
    {
        ListingReview? listing = await views.FindAsync(token, ct);
        return listing is null ? ViewingResult.Missing : new ViewingResult(null, await SlotsAsync(listing.Id, token, ct));
    }

    public async Task<ViewingResult> BookAsync(string token, long slotId, CancellationToken ct)
    {
        ListingReview? listing = await views.FindPersonalAsync(token, ct);
        if (listing is null)
        {
            return ViewingResult.Missing;
        }

        string tokenHash = AccessTokens.Hash(token);
        BookingOutcome outcome = await viewings.BookAsync(listing.Id, slotId, tokenHash, timeProvider.GetUtcNow(), ct);
        IReadOnlyList<ViewingSlot> slots = await SlotsAsync(listing.Id, token, ct);
        if (outcome == BookingOutcome.Booked && slots.FirstOrDefault(slot => slot.Id == slotId) is { } booked)
        {
            await ConfirmAsync(token, listing.Id, booked, tokenHash, ct);
        }

        logger.LogInformation("Viewing slot {SlotId} for listing {ListingId}: {Outcome}", slotId, listing.Id, outcome);
        return new ViewingResult(outcome, slots);
    }

    public async Task<ViewingResult> CancelAsync(string token, CancellationToken ct)
    {
        ListingReview? listing = await views.FindPersonalAsync(token, ct);
        if (listing is null)
        {
            return ViewingResult.Missing;
        }

        bool cancelled = await viewings.CancelAsync(listing.Id, AccessTokens.Hash(token), ct);
        logger.LogInformation("Viewing booking for listing {ListingId} cancelled: {Cancelled}", listing.Id, cancelled);
        return new ViewingResult(null, await SlotsAsync(listing.Id, token, ct));
    }

    private Task<IReadOnlyList<ViewingSlot>> SlotsAsync(Guid listingId, string token, CancellationToken ct) =>
        viewings.GetSlotsAsync(listingId, AccessTokens.Hash(token), timeProvider.GetUtcNow(), ct);

    private async Task ConfirmAsync(string token, Guid listingId, ViewingSlot slot, string tokenHash, CancellationToken ct)
    {
        string text = $"Ihre Besichtigung am {SwissTime.Describe(slot)} ist gebucht. Der Anbieter wurde informiert.";
        await chats.AddAsync(token, listingId, new ChatMessage(ChatMessage.AssistantRole, text, ChatMessage.BookingSource, null, timeProvider.GetUtcNow()), ct);
        await notifier.NotifyAgentOfBookingAsync(listingId, slot, tokenHash, ct);
    }
}
