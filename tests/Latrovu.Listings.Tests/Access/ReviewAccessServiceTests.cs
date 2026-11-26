using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Latrovu.Listings.Tests.Access;

public sealed class ReviewAccessServiceTests
{
    private const string Token = "valid-token";
    private const string WrongCode = "000000";

    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly FakeVerificationCodeSender sender = new();
    private readonly FakeListingAccessTokenStore tokens = new(Token, Guid.NewGuid());
    private readonly FakeListingReviewStore reviews = new();
    private readonly ReviewAccessService service;

    public ReviewAccessServiceTests()
    {
        service = new ReviewAccessService(
            tokens,
            reviews,
            new FakeVerificationCodeStore(time),
            new FakeReviewSessionStore(),
            sender,
            time,
            NullLogger<ReviewAccessService>.Instance);
    }

    [Fact]
    public async Task RequestCode_SendsSixDigitCodeToSourceEmail()
    {
        CodeRequestResult result = await service.RequestCodeAsync(Token, CancellationToken.None);

        Assert.Equal(CodeRequestResult.Sent, result);
        Assert.Equal(FakeListingReviewStore.SourceEmail, sender.LastRecipient);
        Assert.Matches("^[0-9]{6}$", sender.LastCode);
    }

    [Fact]
    public async Task RequestCode_LimitPerWindow_IsEnforced()
    {
        for (int request = 0; request < ReviewAccessService.MaxCodesPerWindow; request++)
        {
            await service.RequestCodeAsync(Token, CancellationToken.None);
        }

        CodeRequestResult result = await service.RequestCodeAsync(Token, CancellationToken.None);

        Assert.Equal(CodeRequestResult.TooManyRequests, result);
    }

    [Fact]
    public async Task VerifyCode_Correct_StartsSessionForThisListing()
    {
        await service.RequestCodeAsync(Token, CancellationToken.None);

        CodeVerificationResult result = await service.VerifyCodeAsync(Token, sender.LastCode!, CancellationToken.None);

        Assert.Equal(CodeVerificationOutcome.Verified, result.Outcome);
        Assert.True(await service.HasValidSessionAsync(Token, result.SessionToken, CancellationToken.None));
        Assert.False(await service.HasValidSessionAsync("other-token", result.SessionToken, CancellationToken.None));
    }

    [Fact]
    public async Task VerifyCode_TooManyWrongAttempts_LocksCode()
    {
        await service.RequestCodeAsync(Token, CancellationToken.None);
        for (int attempt = 0; attempt < ReviewAccessService.MaxAttemptsPerCode; attempt++)
        {
            Assert.Equal(
                CodeVerificationOutcome.Invalid,
                (await service.VerifyCodeAsync(Token, WrongCode, CancellationToken.None)).Outcome);
        }

        CodeVerificationResult result = await service.VerifyCodeAsync(Token, sender.LastCode!, CancellationToken.None);

        Assert.Equal(CodeVerificationOutcome.TooManyAttempts, result.Outcome);
    }

    [Fact]
    public async Task VerifyCode_AfterValidity_IsExpired()
    {
        await service.RequestCodeAsync(Token, CancellationToken.None);
        time.Advance(ReviewAccessService.CodeValidity + TimeSpan.FromSeconds(1));

        CodeVerificationResult result = await service.VerifyCodeAsync(Token, sender.LastCode!, CancellationToken.None);

        Assert.Equal(CodeVerificationOutcome.Expired, result.Outcome);
    }

    [Fact]
    public async Task Session_OpensOtherListingsOfTheSameSenderOnly()
    {
        Guid sameSender = Guid.NewGuid();
        Guid otherSender = Guid.NewGuid();
        tokens.Add("same-sender-token", sameSender);
        tokens.Add("other-sender-token", otherSender);
        reviews.SenderByListing[sameSender] = FakeListingReviewStore.SourceEmail.ToUpperInvariant();
        reviews.SenderByListing[otherSender] = "someone@else.example";
        await service.RequestCodeAsync(Token, CancellationToken.None);

        CodeVerificationResult result = await service.VerifyCodeAsync(Token, sender.LastCode!, CancellationToken.None);

        Assert.True(await service.HasValidSessionAsync("same-sender-token", result.SessionToken, CancellationToken.None));
        Assert.False(await service.HasValidSessionAsync("other-sender-token", result.SessionToken, CancellationToken.None));
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        await service.RequestCodeAsync(Token, CancellationToken.None);
        CodeVerificationResult result = await service.VerifyCodeAsync(Token, sender.LastCode!, CancellationToken.None);

        await service.LogoutAsync(result.SessionToken, CancellationToken.None);

        Assert.False(await service.HasValidSessionAsync(Token, result.SessionToken, CancellationToken.None));
    }

    [Fact]
    public async Task Session_ExpiresAfterValidity()
    {
        await service.RequestCodeAsync(Token, CancellationToken.None);
        CodeVerificationResult result = await service.VerifyCodeAsync(Token, sender.LastCode!, CancellationToken.None);
        time.Advance(ReviewAccessService.SessionValidity + TimeSpan.FromSeconds(1));

        Assert.False(await service.HasValidSessionAsync(Token, result.SessionToken, CancellationToken.None));
    }
}
