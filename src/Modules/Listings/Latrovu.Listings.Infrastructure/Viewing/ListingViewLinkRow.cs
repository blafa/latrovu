namespace Latrovu.Listings.Infrastructure.Viewing;

internal sealed record ListingViewLinkRow(
    string TokenHash,
    Guid ListingId,
    Guid? SearchId,
    DateTimeOffset ExpiresAt,
    string? SeekerAddress,
    string Kind);
