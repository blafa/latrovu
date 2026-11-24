using Latrovu.BuildingBlocks.Messaging;
using Latrovu.Core.Listings;

namespace Latrovu.Searches;

public sealed class SearchRequestHandler(
    ISearchInterpreter interpreter,
    IPlaceResolver placeResolver,
    ISearchStore searches,
    ISeekerNotifier notifier,
    SearchAlerter alerter,
    ILogger<SearchRequestHandler> logger) : IMessageHandler<SearchRequestReceived>
{
    private const string FallbackLanguage = "en";
    private const int MaxInitialAlerts = 5;

    public async Task HandleAsync(SearchRequestReceived message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (await searches.IsProcessedAsync(message.SourceMessageId, ct))
        {
            logger.LogInformation("Search request {RequestId} was already processed, skipping duplicate", message.RequestId);
            return;
        }

        IReadOnlyList<Search> existing = await searches.FindBySeekerAsync(message.SenderAddress, ct);
        SearchInterpretation? interpretation = await interpreter.InterpretAsync(message.Text, existing, ct);
        Search? changed = interpretation is null
            ? null
            : SearchRequestPlanner.Apply(existing, interpretation, message.SenderAddress, message.Channel);
        PlaceResolution places = await SaveAsync(changed, interpretation, ct);

        logger.LogInformation(
            "Search request {RequestId} interpreted as {Action}, search {SearchId}, {PlaceCount} places resolved",
            message.RequestId,
            interpretation?.Action,
            changed?.Id,
            places.Places.Count);
        await notifier.SendReplyAsync(BuildReply(message, interpretation, existing, changed, places), ct);
        await AlertExistingListingsAsync(changed, ct);
        await searches.MarkProcessedAsync(message.SourceMessageId, ct);
    }

    private async Task AlertExistingListingsAsync(Search? search, CancellationToken ct)
    {
        if (search is not { Status: Search.ActiveStatus })
        {
            return;
        }

        IReadOnlyList<ListingReadyForMatching> candidates = await searches.FindCandidatesForSearchAsync(search.Id, MaxInitialAlerts, ct);
        logger.LogInformation("Search {SearchId} matches {CandidateCount} existing listings", search.Id, candidates.Count);
        foreach (ListingReadyForMatching candidate in candidates)
        {
            await alerter.AlertAsync(search, candidate, ct);
        }
    }

    private async Task<PlaceResolution> SaveAsync(Search? changed, SearchInterpretation? interpretation, CancellationToken ct)
    {
        if (changed is null || interpretation is null)
        {
            return PlaceResolution.Empty;
        }

        await searches.SaveAsync(changed, ct);
        if (interpretation.Places.Count == 0)
        {
            return PlaceResolution.Empty;
        }

        PlaceResolution places = await placeResolver.ResolveAsync(interpretation.Places, ct);
        await searches.SetAreaAsync(changed.Id, places.Places, ct);
        return places;
    }

    private static SeekerReply BuildReply(
        SearchRequestReceived message,
        SearchInterpretation? interpretation,
        IReadOnlyList<Search> existing,
        Search? changed,
        PlaceResolution places)
    {
        List<Search> open = existing.Where(search => search.Id != changed?.Id).Append(changed)
            .OfType<Search>()
            .Where(search => search.IsOpen)
            .ToList();
        bool understood = interpretation is not null && interpretation.Action != SearchAction.Unclear;

        return new SeekerReply(
            message.SenderAddress,
            message.Channel,
            interpretation?.Language ?? existing.FirstOrDefault()?.Language ?? FallbackLanguage,
            interpretation?.Summary,
            interpretation?.Refinements ?? [],
            open,
            understood,
            places);
    }
}
