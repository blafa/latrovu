using System.Text.RegularExpressions;

namespace Latrovu.Listings.Extraction.Units;

public static partial class UnitTablePages
{
    private const int MinimumRowsPerPage = 3;
    private const int MinimumAmountsPerRow = 2;

    public static IReadOnlyList<string> Select(IReadOnlyList<string> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);

        List<string> tablePages = pages.Where(page => RowCount(page) >= MinimumRowsPerPage).ToList();
        return tablePages.Count == 0 ? pages : tablePages;
    }

    public static int RowCount(string page) =>
        (page ?? string.Empty).Split('\n').Count(line => FrancAmount().Matches(line).Count >= MinimumAmountsPerRow);

    [GeneratedRegex(@"\b\d{1,3}(?:['’]\d{3})+(?:\.\d{2})?\b|\b\d{3,5}\.\d{2}\b")]
    private static partial Regex FrancAmount();
}
