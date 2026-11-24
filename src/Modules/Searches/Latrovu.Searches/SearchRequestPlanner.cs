namespace Latrovu.Searches;

public static class SearchRequestPlanner
{
    public const string FallbackName = "Suche";

    public static Search? Apply(
        IReadOnlyList<Search> existing,
        SearchInterpretation interpretation,
        string seekerAddress,
        string channel)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(interpretation);

        Search? target = FindTarget(existing, interpretation);
        return interpretation.Action switch
        {
            SearchAction.New or SearchAction.Update when target is null =>
                Create(interpretation, seekerAddress, channel),
            SearchAction.New or SearchAction.Update =>
                target! with { CriteriaJson = interpretation.CriteriaJson, Language = interpretation.Language, Status = Search.ActiveStatus },
            SearchAction.Pause => target is null ? null : target with { Status = Search.PausedStatus },
            SearchAction.Resume => target is null ? null : target with { Status = Search.ActiveStatus },
            SearchAction.Stop => target is null ? null : target with { Status = Search.StoppedStatus },
            _ => null
        };
    }

    private static Search? FindTarget(IReadOnlyList<Search> existing, SearchInterpretation interpretation)
    {
        List<Search> open = existing.Where(search => search.IsOpen).ToList();
        Search? byName = open.FirstOrDefault(search =>
            string.Equals(search.Name, interpretation.SearchName?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (byName is not null || interpretation.Action == SearchAction.New)
        {
            return byName;
        }

        return open.Count == 1 ? open[0] : null;
    }

    private static Search Create(SearchInterpretation interpretation, string seekerAddress, string channel)
    {
        string name = string.IsNullOrWhiteSpace(interpretation.SearchName) ? FallbackName : interpretation.SearchName.Trim();
        return new Search(
            Guid.NewGuid(),
            seekerAddress,
            channel,
            name,
            interpretation.Language,
            interpretation.CriteriaJson,
            Search.ActiveStatus);
    }
}
