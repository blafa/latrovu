namespace Latrovu.Searches;

public interface ISearchInterpreter
{
    Task<SearchInterpretation?> InterpretAsync(string text, IReadOnlyList<Search> existingSearches, CancellationToken ct);
}
