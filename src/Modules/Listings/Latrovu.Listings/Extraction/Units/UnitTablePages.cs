namespace Latrovu.Listings.Extraction.Units;

public static class UnitTablePages
{
    private const int MinimumRowsPerPage = 3;
    private const int MinimumAmountsPerRow = 2;

    public static IReadOnlyList<string> Select(IReadOnlyList<string> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);

        List<string> tablePages = pages.Where(page => RowCount(page) >= MinimumRowsPerPage).ToList();
        return tablePages.Count == 0 ? pages : tablePages;
    }

    public static int RowCount(string page) => (page ?? string.Empty).Split('\n').Count(IsTableRow);

    public static bool IsTableRow(string line) => FrancAmounts.In(line).Count >= MinimumAmountsPerRow;
}
