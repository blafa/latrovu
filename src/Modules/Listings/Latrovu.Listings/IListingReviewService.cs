namespace Latrovu.Listings;

public interface IListingReviewService
{
    Task<ListingReview?> GetByTokenAsync(string token, CancellationToken ct);

    Task<FieldUpdateResult> UpdateFieldAsync(string token, string path, string? rawValue, CancellationToken ct);

    Task<bool> ApproveAsync(string token, CancellationToken ct);
}
