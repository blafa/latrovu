using Latrovu.BuildingBlocks.Messaging;
using Latrovu.BuildingBlocks.Security;
using Latrovu.Core.Listings;

namespace Latrovu.Searches;

public sealed class ListingMatchingHandler(
    ISearchStore searches,
    ISeekerNotifier notifier,
    TimeProvider timeProvider,
    ILogger<ListingMatchingHandler> logger) : IMessageHandler<ListingReadyForMatching>
{
    public static readonly TimeSpan LinkValidity = TimeSpan.FromDays(30);

    public async Task HandleAsync(ListingReadyForMatching message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        IReadOnlyList<Search> matches = await searches.FindMatchesAsync(message, ct);
        logger.LogInformation("Listing {ListingId} matches {MatchCount} searches", message.ListingId, matches.Count);
        foreach (Search search in matches)
        {
            await AlertAsync(search, message, ct);
        }
    }

    private async Task AlertAsync(Search search, ListingReadyForMatching listing, CancellationToken ct)
    {
        string token = SecureTokens.Create();
        ViewLinkRequested linkRequest = new(listing.ListingId, search.Id, SecureTokens.Hash(token), timeProvider.GetUtcNow() + LinkValidity);
        if (!await searches.RecordAlertAsync(linkRequest, ct))
        {
            logger.LogInformation("Search {SearchId} was already alerted for listing {ListingId}", search.Id, listing.ListingId);
            return;
        }

        await notifier.SendAlertAsync(new SeekerAlert(search.SeekerAddress, search.Channel, search.Language, search.Name, listing, token), ct);
        logger.LogInformation("Search {SearchId} alerted for listing {ListingId}", search.Id, listing.ListingId);
    }
}
