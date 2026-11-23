namespace Latrovu.Listings.Access;

public interface IVerificationCodeStore
{
    Task<int> CountCreatedSinceAsync(Guid listingId, DateTimeOffset since, CancellationToken ct);

    Task SaveAsync(Guid listingId, string codeHash, DateTimeOffset expiresAt, CancellationToken ct);

    Task<ActiveVerificationCode?> FindLatestActiveAsync(Guid listingId, DateTimeOffset now, CancellationToken ct);

    Task IncrementAttemptsAsync(Guid codeId, CancellationToken ct);

    Task DeleteAllAsync(Guid listingId, CancellationToken ct);
}
