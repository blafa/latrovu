namespace Latrovu.Searches;

public static class SearchRequestPlanner
{
    public const string FallbackName = "Suche";

    public static IReadOnlyList<Search> Apply(
        IReadOnlyList<Search> existing,
        SearchInterpretation interpretation,
        string seekerAddress,
        string channel,
        Guid? repliedSearchId = null)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(interpretation);

        string? status = StatusFor(interpretation.Action);
        if (interpretation.AppliesToAll && status is not null)
        {
            return existing.Where(search => search.IsOpen).Select(search => search with { Status = status }).ToList();
        }

        Search? target = FindTarget(existing, interpretation, repliedSearchId);
        Search? changed = ApplyToOne(target, interpretation, seekerAddress, channel, status);
        return changed is null ? [] : [changed];
    }

    private static Search? ApplyToOne(
        Search? target,
        SearchInterpretation interpretation,
        string seekerAddress,
        string channel,
        string? status) =>
        interpretation.Action switch
        {
            SearchAction.New or SearchAction.Update when target is null =>
                Create(interpretation, seekerAddress, channel),
            SearchAction.New or SearchAction.Update =>
                target! with { CriteriaJson = interpretation.CriteriaJson, Language = interpretation.Language, Status = Search.ActiveStatus },
            _ when status is not null && target is not null => target with { Status = status },
            _ => null
        };

    private static string? StatusFor(SearchAction action) => action switch
    {
        SearchAction.Pause => Search.PausedStatus,
        SearchAction.Resume => Search.ActiveStatus,
        SearchAction.Stop => Search.StoppedStatus,
        _ => null
    };

    private static Search? FindTarget(IReadOnlyList<Search> existing, SearchInterpretation interpretation, Guid? repliedSearchId)
    {
        List<Search> open = existing.Where(search => search.IsOpen).ToList();
        Search? byName = open.FirstOrDefault(search =>
            string.Equals(search.Name, interpretation.SearchName?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (byName is not null || interpretation.Action == SearchAction.New || !string.IsNullOrWhiteSpace(interpretation.SearchName))
        {
            return byName;
        }

        Search? replied = open.FirstOrDefault(search => search.Id == repliedSearchId);
        return replied ?? (open.Count == 1 ? open[0] : null);
    }

    private static Search Create(SearchInterpretation interpretation, string seekerAddress, string channel)
    {
        string? name = string.IsNullOrWhiteSpace(interpretation.SearchName)
            ? interpretation.Places.FirstOrDefault()?.Name
            : interpretation.SearchName;
        return new Search(
            Guid.NewGuid(),
            seekerAddress,
            channel,
            string.IsNullOrWhiteSpace(name) ? FallbackName : name.Trim(),
            interpretation.Language,
            interpretation.CriteriaJson,
            Search.ActiveStatus);
    }
}
