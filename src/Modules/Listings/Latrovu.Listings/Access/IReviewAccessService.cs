namespace Latrovu.Listings.Access;

public interface IReviewAccessService
{
    Task<CodeRequestResult> RequestCodeAsync(string token, CancellationToken ct);

    Task<CodeVerificationResult> VerifyCodeAsync(string token, string code, CancellationToken ct);

    Task<bool> HasValidSessionAsync(string token, string? sessionToken, CancellationToken ct);
}
