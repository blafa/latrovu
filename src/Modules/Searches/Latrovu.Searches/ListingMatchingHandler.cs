using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;

namespace Latrovu.Searches;

public sealed class ListingMatchingHandler(
    ISearchStore searches,
    SearchAlerter alerter,
    ILogger<ListingMatchingHandler> logger) : IMessageHandler<ListingReadyForMatching>
{
    public async Task HandleAsync(ListingReadyForMatching message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        await searches.SaveCandidateAsync(message, ct);
        IReadOnlyList<Search> matches = await searches.FindMatchesAsync(message, ct);
        logger.LogInformation("Listing {ListingId} matches {MatchCount} searches", message.ListingId, matches.Count);
        foreach (Search search in matches)
        {
            await alerter.AlertAsync(search, message, ct);
        }
    }
}
