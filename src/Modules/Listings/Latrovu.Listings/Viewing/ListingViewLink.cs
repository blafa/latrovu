namespace Latrovu.Listings.Viewing;

public sealed record ListingViewLink(Guid ListingId, bool IsShared, DateTimeOffset ExpiresAt);
