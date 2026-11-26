using System.Globalization;
using System.Security.Cryptography;

namespace Latrovu.Listings.Access;

public sealed class ReviewAccessService(
    IListingAccessTokenStore tokens,
    IListingReviewStore reviews,
    IVerificationCodeStore codes,
    IReviewSessionStore sessions,
    IVerificationCodeSender sender,
    TimeProvider timeProvider,
    ILogger<ReviewAccessService> logger) : IReviewAccessService
{
    public const int MaxAttemptsPerCode = 5;
    public const int MaxCodesPerWindow = 3;
    public static readonly TimeSpan CodeValidity = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan CodeRequestWindow = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan SessionValidity = TimeSpan.FromHours(12);
    private const int CodeUpperBound = 1_000_000;
    private const string CodeFormat = "D6";

    public async Task<CodeRequestResult> RequestCodeAsync(string token, CancellationToken ct)
    {
        Guid? listingId = await tokens.FindListingIdAsync(token, ct);
        ListingReview? review = listingId is null ? null : await reviews.GetAsync(listingId.Value, ct);
        if (review is null)
        {
            return CodeRequestResult.NotFound;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        if (await codes.CountCreatedSinceAsync(review.Id, now - CodeRequestWindow, ct) >= MaxCodesPerWindow)
        {
            logger.LogWarning("Code request limit reached for listing {ListingId}", review.Id);
            return CodeRequestResult.TooManyRequests;
        }

        string code = RandomNumberGenerator.GetInt32(CodeUpperBound).ToString(CodeFormat, CultureInfo.InvariantCulture);
        await codes.SaveAsync(review.Id, HashCode(review.Id, code), now + CodeValidity, ct);
        await sender.SendAsync(review.SourceEmail, code, ct);
        logger.LogInformation("Verification code sent for listing {ListingId}", review.Id);
        return CodeRequestResult.Sent;
    }

    public async Task<CodeVerificationResult> VerifyCodeAsync(string token, string code, CancellationToken ct)
    {
        Guid? listingId = await tokens.FindListingIdAsync(token, ct);
        if (listingId is null)
        {
            return CodeVerificationResult.Failed(CodeVerificationOutcome.NotFound);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        ActiveVerificationCode? active = await codes.FindLatestActiveAsync(listingId.Value, now, ct);
        CodeVerificationOutcome? failure = await CheckCodeAsync(active, listingId.Value, code, ct);
        if (failure is not null)
        {
            logger.LogWarning("Code verification for listing {ListingId} failed: {Outcome}", listingId, failure);
            return CodeVerificationResult.Failed(failure.Value);
        }

        return await StartSessionAsync(listingId.Value, now, ct);
    }

    public async Task<bool> HasValidSessionAsync(string token, string? sessionToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return false;
        }

        string? sessionEmail = await sessions.FindSourceEmailAsync(AccessTokens.Hash(sessionToken), timeProvider.GetUtcNow(), ct);
        Guid? listingId = sessionEmail is null ? null : await tokens.FindListingIdAsync(token, ct);
        ListingReview? review = listingId is null ? null : await reviews.GetAsync(listingId.Value, ct);
        return review is not null && string.Equals(review.SourceEmail, sessionEmail, StringComparison.OrdinalIgnoreCase);
    }

    public async Task LogoutAsync(string? sessionToken, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(sessionToken))
        {
            await sessions.DeleteAsync(AccessTokens.Hash(sessionToken), ct);
            logger.LogInformation("Review session ended by the agent");
        }
    }

    private async Task<CodeVerificationOutcome?> CheckCodeAsync(
        ActiveVerificationCode? active,
        Guid listingId,
        string code,
        CancellationToken ct)
    {
        if (active is null)
        {
            return CodeVerificationOutcome.Expired;
        }

        if (active.Attempts >= MaxAttemptsPerCode)
        {
            return CodeVerificationOutcome.TooManyAttempts;
        }

        if (string.IsNullOrWhiteSpace(code) || HashCode(listingId, code.Trim()) != active.CodeHash)
        {
            await codes.IncrementAttemptsAsync(active.Id, ct);
            return CodeVerificationOutcome.Invalid;
        }

        return null;
    }

    private async Task<CodeVerificationResult> StartSessionAsync(Guid listingId, DateTimeOffset now, CancellationToken ct)
    {
        ListingReview? review = await reviews.GetAsync(listingId, ct);
        if (review is null)
        {
            return CodeVerificationResult.Failed(CodeVerificationOutcome.NotFound);
        }

        await codes.DeleteAllAsync(listingId, ct);
        string sessionToken = AccessTokens.Create();
        await sessions.CreateAsync(review.SourceEmail, AccessTokens.Hash(sessionToken), now + SessionValidity, ct);
        logger.LogInformation("Review session started via listing {ListingId}, valid for all listings of its sender", listingId);
        return new CodeVerificationResult(CodeVerificationOutcome.Verified, sessionToken);
    }

    private static string HashCode(Guid listingId, string code) => AccessTokens.Hash($"{listingId}:{code}");
}
