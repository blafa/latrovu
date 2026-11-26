namespace Latrovu.Listings.Extraction.Units;

public sealed record UnitTableInput(string? Header, IReadOnlyList<string> Rows)
{
    public const int MinimumRows = 3;

    public bool HasRows => Rows.Count >= MinimumRows;

    public string ToPrompt() =>
        (Header is null ? string.Empty : $"Kopfzeile: {Header}\n")
        + string.Join('\n', Rows.Select((row, index) => $"Zeile {index + 1}: {row}"));

    public static UnitTableInput Find(IReadOnlyList<string> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);

        string? header = null;
        List<string> rows = [];
        foreach (string page in pages)
        {
            string[] lines = (page ?? string.Empty).Split('\n').Select(line => line.Trim()).ToArray();
            for (int index = 0; index < lines.Length; index++)
            {
                if (!UnitTablePages.IsTableRow(lines[index]))
                {
                    continue;
                }

                header ??= HeaderAbove(lines, index);
                rows.Add(lines[index]);
            }
        }

        return new UnitTableInput(header, rows);
    }

    private static string? HeaderAbove(string[] lines, int firstRow) =>
        firstRow > 0 && lines[firstRow - 1].Any(char.IsLetter) && !UnitTablePages.IsTableRow(lines[firstRow - 1])
            ? lines[firstRow - 1]
            : null;
}
