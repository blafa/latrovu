namespace Latrovu.Listings.Tests.Fakes;

internal sealed class FakeReviewSessionStore : IReviewSessionStore
{
    private readonly Dictionary<string, (string SourceEmail, DateTimeOffset ExpiresAt)> sessions = [];

    public Task CreateAsync(string sourceEmail, string sessionHash, DateTimeOffset expiresAt, CancellationToken ct)
    {
        sessions[sessionHash] = (sourceEmail, expiresAt);
        return Task.CompletedTask;
    }

    public Task<string?> FindSourceEmailAsync(string sessionHash, DateTimeOffset now, CancellationToken ct) =>
        Task.FromResult(
            sessions.TryGetValue(sessionHash, out (string SourceEmail, DateTimeOffset ExpiresAt) session) && session.ExpiresAt > now
                ? session.SourceEmail
                : null);

    public Task DeleteAsync(string sessionHash, CancellationToken ct)
    {
        sessions.Remove(sessionHash);
        return Task.CompletedTask;
    }
}
