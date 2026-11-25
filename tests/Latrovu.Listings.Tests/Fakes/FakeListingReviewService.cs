namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeListingReviewService(Guid listingId) : IListingReviewService
{
    public const string ValidToken = "review-token";

    public Task<ListingReview?> GetByTokenAsync(string token, CancellationToken ct) =>
        Task.FromResult(token == ValidToken
            ? new ListingReview(listingId, ListingReview.ApprovedStatus, FakeListingReviewStore.SourceEmail, null, "{}", "{}", "{}", false, [])
            : null);

    public Task<FieldUpdateResult> UpdateFieldAsync(string token, string path, string? rawValue, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<FieldUpdateResult> ConfirmFieldAsync(string token, string path, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<FieldUpdateResult> AddCustomDetailAsync(string token, string label, string value, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<bool> SetPublishContactAsync(string token, bool publishContact, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<bool> ApproveAsync(string token, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<bool> WithdrawAsync(string token, CancellationToken ct) =>
        throw new NotSupportedException();
}
