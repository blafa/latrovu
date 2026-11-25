namespace Latrovu.Listings.Renewal;

public interface IListingRenewalStore
{
    Task<IReadOnlyList<RenewalState>> GetApprovedAsync(CancellationToken ct);

    Task MarkReminderSentAsync(Guid listingId, int number, DateTimeOffset now, CancellationToken ct);

    Task<string> CreateTokenAsync(Guid listingId, DateTimeOffset expiresAt, CancellationToken ct);

    Task<Guid?> FindListingByTokenAsync(string token, DateTimeOffset now, CancellationToken ct);

    Task<bool> RenewAsync(Guid listingId, DateTimeOffset now, CancellationToken ct);
}
