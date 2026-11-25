namespace Latrovu.Listings.Viewing.Viewings;

public sealed class AgentViewingService(
    IListingReviewService reviews,
    IViewingStore viewings,
    TimeProvider timeProvider,
    ILogger<AgentViewingService> logger)
{
    public const int MaxCapacity = 50;

    public async Task<ViewingResult> GetAsync(string reviewToken, CancellationToken ct)
    {
        ListingReview? review = await reviews.GetByTokenAsync(reviewToken, ct);
        return review is null ? ViewingResult.Missing : new ViewingResult(null, await SlotsAsync(review.Id, ct));
    }

    public async Task<ViewingResult> AddAsync(string reviewToken, DateTimeOffset startsAt, DateTimeOffset endsAt, int capacity, CancellationToken ct)
    {
        ListingReview? review = await reviews.GetByTokenAsync(reviewToken, ct);
        if (review is null)
        {
            return ViewingResult.Missing;
        }

        bool valid = endsAt > startsAt && startsAt > timeProvider.GetUtcNow() && capacity is >= 1 and <= MaxCapacity;
        if (valid)
        {
            long slotId = await viewings.AddSlotAsync(review.Id, startsAt, endsAt, capacity, ct);
            logger.LogInformation("Viewing slot {SlotId} added to listing {ListingId}", slotId, review.Id);
        }

        return new ViewingResult(valid ? null : BookingOutcome.NotFound, await SlotsAsync(review.Id, ct));
    }

    public async Task<ViewingResult> RemoveAsync(string reviewToken, long slotId, CancellationToken ct)
    {
        ListingReview? review = await reviews.GetByTokenAsync(reviewToken, ct);
        if (review is null)
        {
            return ViewingResult.Missing;
        }

        bool removed = await viewings.RemoveEmptySlotAsync(review.Id, slotId, ct);
        logger.LogInformation("Viewing slot {SlotId} of listing {ListingId} removed: {Removed}", slotId, review.Id, removed);
        return new ViewingResult(null, await SlotsAsync(review.Id, ct));
    }

    private Task<IReadOnlyList<ViewingSlot>> SlotsAsync(Guid listingId, CancellationToken ct) =>
        viewings.GetSlotsAsync(listingId, null, timeProvider.GetUtcNow(), ct);
}
