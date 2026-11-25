namespace Latrovu.Core.Listings;

public sealed record ViewLinkRequested(
    Guid ListingId,
    Guid SearchId,
    string TokenHash,
    DateTimeOffset ExpiresAt,
    string? SeekerAddress = null);
