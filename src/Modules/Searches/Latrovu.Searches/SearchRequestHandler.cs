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
    private const int ShortMessageLength = 20;

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
        IReadOnlyList<Search> changed = interpretation is null
            ? []
            : SearchRequestPlanner.Apply(existing, interpretation, message.SenderAddress, message.Channel);
        PlaceResolution places = await SaveAsync(changed, interpretation, ct);

        logger.LogInformation(
            "Search request {RequestId} interpreted as {Action}, {ChangedCount} searches changed, {PlaceCount} places resolved",
            message.RequestId,
            interpretation?.Action,
            changed.Count,
            places.Places.Count);
        await notifier.SendReplyAsync(BuildReply(message, interpretation, existing, changed, places), ct);
        await AlertExistingListingsAsync(changed, ct);
        await searches.MarkProcessedAsync(message.SourceMessageId, ct);
    }

    private async Task<PlaceResolution> SaveAsync(IReadOnlyList<Search> changed, SearchInterpretation? interpretation, CancellationToken ct)
    {
        foreach (Search search in changed)
        {
            await searches.SaveAsync(search, ct);
        }

        bool definesPlaces = interpretation is { Action: SearchAction.New or SearchAction.Update, Places.Count: > 0 };
        if (changed.Count != 1 || !definesPlaces)
        {
            return PlaceResolution.Empty;
        }

        PlaceResolution places = await placeResolver.ResolveAsync(interpretation!.Places, ct);
        await searches.SetAreaAsync(changed[0].Id, places.Places, ct);
        return places;
    }

    private async Task AlertExistingListingsAsync(IReadOnlyList<Search> changed, CancellationToken ct)
    {
        foreach (Search search in changed.Where(search => search.Status == Search.ActiveStatus))
        {
            IReadOnlyList<ListingReadyForMatching> candidates = await searches.FindCandidatesForSearchAsync(search.Id, MaxInitialAlerts, ct);
            logger.LogInformation("Search {SearchId} matches {CandidateCount} existing listings", search.Id, candidates.Count);
            foreach (ListingReadyForMatching candidate in candidates)
            {
                await alerter.AlertAsync(search, candidate, ct);
            }
        }
    }

    private static SeekerReply BuildReply(
        SearchRequestReceived message,
        SearchInterpretation? interpretation,
        IReadOnlyList<Search> existing,
        IReadOnlyList<Search> changed,
        PlaceResolution places)
    {
        HashSet<Guid> changedIds = changed.Select(search => search.Id).ToHashSet();
        List<Search> open = existing.Where(search => !changedIds.Contains(search.Id))
            .Concat(changed)
            .Where(search => search.IsOpen)
            .ToList();
        bool understood = interpretation is not null && interpretation.Action != SearchAction.Unclear;

        return new SeekerReply(
            message.SenderAddress,
            message.Channel,
            ReplyLanguage(message, interpretation, existing),
            interpretation?.Summary,
            interpretation?.Refinements ?? [],
            open,
            understood,
            places);
    }

    private static string ReplyLanguage(SearchRequestReceived message, SearchInterpretation? interpretation, IReadOnlyList<Search> existing)
    {
        string? known = existing.FirstOrDefault()?.Language;
        bool tooShortToTell = message.Text.Trim().Length < ShortMessageLength;
        return (tooShortToTell ? known : null) ?? interpretation?.Language ?? known ?? FallbackLanguage;
    }
}
