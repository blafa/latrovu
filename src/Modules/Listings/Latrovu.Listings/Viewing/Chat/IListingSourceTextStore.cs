namespace Latrovu.Listings.Viewing.Chat;

public interface IListingSourceTextStore
{
    Task<IReadOnlyList<string>> GetPagesAsync(Guid listingId, CancellationToken ct);
}
