namespace Latrovu.Listings.Tests;

internal sealed class StoredCode(Guid id, Guid listingId, string codeHash, DateTimeOffset createdAt, DateTimeOffset expiresAt)
{
    public Guid Id { get; } = id;

    public Guid ListingId { get; } = listingId;

    public string CodeHash { get; } = codeHash;

    public DateTimeOffset CreatedAt { get; } = createdAt;

    public DateTimeOffset ExpiresAt { get; } = expiresAt;

    public int Attempts { get; set; }
}
