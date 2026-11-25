using Latrovu.Listings.Renewal;

namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeListingRenewalStore : IListingRenewalStore
{
    private readonly Dictionary<Guid, RenewalState> states = [];
    private readonly Dictionary<string, (Guid ListingId, DateTimeOffset ExpiresAt)> tokens = [];
    private int nextToken;

    public RenewalState this[Guid listingId] => states[listingId];

    public void Add(RenewalState state) => states[state.ListingId] = state;

    public Task<IReadOnlyList<RenewalState>> GetApprovedAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<RenewalState>>(states.Values.ToList());

    public Task MarkReminderSentAsync(Guid listingId, int number, DateTimeOffset now, CancellationToken ct)
    {
        states[listingId] = states[listingId] with { RemindersSent = number, LastReminderAt = now };
        return Task.CompletedTask;
    }

    public Task<string> CreateTokenAsync(Guid listingId, DateTimeOffset expiresAt, CancellationToken ct)
    {
        string token = $"renew-{++nextToken}";
        tokens[token] = (listingId, expiresAt);
        return Task.FromResult(token);
    }

    public Task<Guid?> FindListingByTokenAsync(string token, DateTimeOffset now, CancellationToken ct) =>
        Task.FromResult<Guid?>(tokens.TryGetValue(token, out (Guid ListingId, DateTimeOffset ExpiresAt) entry) && entry.ExpiresAt > now ? entry.ListingId : null);

    public Task<bool> RenewAsync(Guid listingId, DateTimeOffset now, CancellationToken ct)
    {
        if (!states.TryGetValue(listingId, out RenewalState? state))
        {
            return Task.FromResult(false);
        }

        states[listingId] = state with { RenewedAt = now, RemindersSent = 0, LastReminderAt = null };
        return Task.FromResult(true);
    }
}
