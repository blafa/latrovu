namespace Latrovu.Core.Listings;

public sealed record ListingWithdrawn(Guid ListingId, IReadOnlyList<string>? SourceMessageIds = null);
