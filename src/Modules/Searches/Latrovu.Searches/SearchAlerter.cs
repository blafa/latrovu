using Latrovu.BuildingBlocks.Security;
using Latrovu.Core.Listings;

namespace Latrovu.Searches;

public sealed class SearchAlerter(
    ISearchStore searches,
    ISeekerNotifier notifier,
    TimeProvider timeProvider,
    ILogger<SearchAlerter> logger)
{
    public static readonly TimeSpan LinkValidity = TimeSpan.FromDays(30);

    public async Task<bool> AlertAsync(Search search, ListingReadyForMatching listing, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(search);
        ArgumentNullException.ThrowIfNull(listing);

        string token = SecureTokens.Create();
        ViewLinkRequested linkRequest = new(listing.ListingId, search.Id, SecureTokens.Hash(token), timeProvider.GetUtcNow() + LinkValidity, search.SeekerAddress);
        if (!await searches.RecordAlertAsync(linkRequest, ct))
        {
            logger.LogInformation("Search {SearchId} was already alerted for listing {ListingId}", search.Id, listing.ListingId);
            return false;
        }

        string? reference = await notifier.SendAlertAsync(new SeekerAlert(search.SeekerAddress, search.Channel, search.Language, search.Name, listing, token), ct);
        if (reference is not null)
        {
            await searches.RecordSentMessageAsync(reference, search.Id, ct);
        }

        logger.LogInformation("Search {SearchId} alerted for listing {ListingId}", search.Id, listing.ListingId);
        return true;
    }
}
