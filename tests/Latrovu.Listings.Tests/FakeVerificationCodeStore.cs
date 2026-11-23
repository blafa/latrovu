using Latrovu.Listings;

namespace Latrovu.Listings.Tests;

internal sealed class FakeVerificationCodeStore(TimeProvider timeProvider) : IVerificationCodeStore
{
    private readonly List<StoredCode> codes = [];

    public Task<int> CountCreatedSinceAsync(Guid listingId, DateTimeOffset since, CancellationToken ct) =>
        Task.FromResult(codes.Count(code => code.ListingId == listingId && code.CreatedAt >= since));

    public Task SaveAsync(Guid listingId, string codeHash, DateTimeOffset expiresAt, CancellationToken ct)
    {
        codes.Add(new StoredCode(Guid.NewGuid(), listingId, codeHash, timeProvider.GetUtcNow(), expiresAt));
        return Task.CompletedTask;
    }

    public Task<ActiveVerificationCode?> FindLatestActiveAsync(Guid listingId, DateTimeOffset now, CancellationToken ct)
    {
        StoredCode? latest = codes
            .Where(code => code.ListingId == listingId && code.ExpiresAt > now)
            .OrderByDescending(code => code.CreatedAt)
            .FirstOrDefault();
        return Task.FromResult(latest is null ? null : new ActiveVerificationCode(latest.Id, latest.CodeHash, latest.Attempts));
    }

    public Task IncrementAttemptsAsync(Guid codeId, CancellationToken ct)
    {
        codes.Single(code => code.Id == codeId).Attempts++;
        return Task.CompletedTask;
    }

    public Task DeleteAllAsync(Guid listingId, CancellationToken ct)
    {
        codes.RemoveAll(code => code.ListingId == listingId);
        return Task.CompletedTask;
    }
}
