namespace Latrovu.Listings.Review;

public interface IListingReviewService
{
    Task<ListingReview?> GetByTokenAsync(string token, CancellationToken ct);

    Task<FieldUpdateResult> UpdateFieldAsync(string token, string path, string? rawValue, CancellationToken ct);

    Task<FieldUpdateResult> ConfirmFieldAsync(string token, string path, CancellationToken ct);

    Task<FieldUpdateResult> AddCustomDetailAsync(string token, string label, string value, CancellationToken ct);

    Task<bool> SetPublishContactAsync(string token, bool publishContact, CancellationToken ct);

    Task<bool> ApproveAsync(string token, CancellationToken ct);

    Task<bool> WithdrawAsync(string token, CancellationToken ct);
}
