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
        Guid? repliedSearchId = await RepliedSearchAsync(message, ct);
        SearchInterpretation? interpretation = await interpreter.InterpretAsync(message.Text, existing, ct);
        IReadOnlyList<Search> changed = interpretation is null
            ? []
            : SearchRequestPlanner.Apply(existing, interpretation, message.SenderAddress, message.Channel, repliedSearchId);
        PlaceResolution places = await SaveAsync(changed, interpretation, ct);
        LogInterpretation(message, interpretation, changed, places);

        SearchMatchSummary? matches = await AlertExistingListingsAsync(changed, ct);
        string? reference = await notifier.SendReplyAsync(BuildReply(message, interpretation, existing, changed, places, matches), ct);
        await RecordReplyAsync(reference, changed, ct);
        await searches.MarkProcessedAsync(message.SourceMessageId, ct);
    }

    private async Task<Guid?> RepliedSearchAsync(SearchRequestReceived message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.ReplyToReference))
        {
            return null;
        }

        Guid? searchId = await searches.FindSearchBySentMessageAsync(message.ReplyToReference, ct);
        logger.LogInformation("Search request {RequestId} replies to a message about search {SearchId}", message.RequestId, searchId);
        return searchId;
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

    private void LogInterpretation(
        SearchRequestReceived message,
        SearchInterpretation? interpretation,
        IReadOnlyList<Search> changed,
        PlaceResolution places) =>
        logger.LogInformation(
            "Search request {RequestId} interpreted as {Action}, {ChangedCount} searches changed, {PlaceCount} places resolved",
            message.RequestId,
            interpretation?.Action,
            changed.Count,
            places.Places.Count);

    private async Task<SearchMatchSummary?> AlertExistingListingsAsync(IReadOnlyList<Search> changed, CancellationToken ct)
    {
        List<Search> active = changed.Where(search => search.Status == Search.ActiveStatus).ToList();
        if (active.Count == 0)
        {
            return null;
        }

        int alreadySent = 0;
        int sent = 0;
        foreach (Search search in active)
        {
            alreadySent += await searches.CountAlertedMatchesAsync(search.Id, ct);
            sent += await AlertCandidatesAsync(search, ct);
        }

        return new SearchMatchSummary(sent, alreadySent);
    }

    private async Task<int> AlertCandidatesAsync(Search search, CancellationToken ct)
    {
        IReadOnlyList<ListingReadyForMatching> candidates = await searches.FindCandidatesForSearchAsync(search.Id, MaxInitialAlerts, ct);
        logger.LogInformation("Search {SearchId} matches {CandidateCount} existing listings", search.Id, candidates.Count);
        int sent = 0;
        foreach (ListingReadyForMatching candidate in candidates)
        {
            sent += await alerter.AlertAsync(search, candidate, ct) ? 1 : 0;
        }

        return sent;
    }

    private async Task RecordReplyAsync(string? reference, IReadOnlyList<Search> changed, CancellationToken ct)
    {
        if (reference is null || changed.Count != 1)
        {
            return;
        }

        await searches.RecordSentMessageAsync(reference, changed[0].Id, ct);
    }

    private static SeekerReply BuildReply(
        SearchRequestReceived message,
        SearchInterpretation? interpretation,
        IReadOnlyList<Search> existing,
        IReadOnlyList<Search> changed,
        PlaceResolution places,
        SearchMatchSummary? matches)
    {
        HashSet<Guid> changedIds = changed.Select(search => search.Id).ToHashSet();
        List<Search> open = existing.Where(search => !changedIds.Contains(search.Id))
            .Concat(changed)
            .Where(search => search.IsOpen)
            .ToList();
        bool understood = interpretation is not null && interpretation.Action != SearchAction.Unclear;
        bool changesStatus = interpretation?.Action is SearchAction.Pause or SearchAction.Resume or SearchAction.Stop;

        return new SeekerReply(
            message.SenderAddress,
            message.Channel,
            ReplyLanguage(message, interpretation, existing),
            interpretation?.Summary,
            interpretation?.Refinements ?? [],
            open,
            understood,
            places,
            changesStatus ? changed : [],
            matches);
    }

    private static string ReplyLanguage(SearchRequestReceived message, SearchInterpretation? interpretation, IReadOnlyList<Search> existing)
    {
        string? known = existing.FirstOrDefault()?.Language;
        bool tooShortToTell = message.Text.Trim().Length < ShortMessageLength;
        return (tooShortToTell ? known : null) ?? interpretation?.Language ?? known ?? FallbackLanguage;
    }
}
