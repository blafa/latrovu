namespace Latrovu.BuildingBlocks.Infrastructure.Pdf;

public static class LineAssembler
{
    private const double MinimumTolerance = 2.0;
    private const double ToleranceShareOfHeight = 0.5;

    public static string Assemble(IEnumerable<PositionedWord> words)
    {
        ArgumentNullException.ThrowIfNull(words);

        List<List<PositionedWord>> lines = [];
        foreach (PositionedWord word in words.Where(word => !string.IsNullOrWhiteSpace(word.Text)).OrderByDescending(word => word.Bottom).ThenBy(word => word.Left))
        {
            List<PositionedWord>? line = lines.Count == 0 ? null : lines[^1];
            if (line is not null && IsSameLine(line[0], word))
            {
                line.Add(word);
            }
            else
            {
                lines.Add([word]);
            }
        }

        return string.Join('\n', lines.Select(line => string.Join(' ', line.OrderBy(word => word.Left).Select(word => word.Text))));
    }

    private static bool IsSameLine(PositionedWord first, PositionedWord word)
    {
        double tolerance = Math.Max(MinimumTolerance, Math.Min(first.Height, word.Height) * ToleranceShareOfHeight);
        return Math.Abs(first.Bottom - word.Bottom) <= tolerance;
    }
}
