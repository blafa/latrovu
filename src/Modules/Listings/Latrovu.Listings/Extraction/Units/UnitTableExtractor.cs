using Latrovu.BuildingBlocks.Ai;

namespace Latrovu.Listings.Extraction.Units;

public sealed class UnitTableExtractor(ILlmClient llm, ILogger<UnitTableExtractor> logger) : IUnitTableExtractor
{
    private const char PageSeparator = '\n';

    public async Task<IReadOnlyList<ListingUnit>> ExtractAsync(IReadOnlyList<string> pages, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pages);

        UnitTableInput table = UnitTableInput.Find(pages);
        string content = table.HasRows ? table.ToPrompt() : string.Join(PageSeparator, UnitTablePages.Select(pages));
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        string? answer = await llm.CompleteAsync(UnitTablePrompt.Text, content, UnitTableSchema.Value, ct);
        IReadOnlyList<ListingUnit> units = UnitTableParser.Parse(LlmJsonNormalizer.Normalize(answer), table.HasRows ? table.Rows : null);
        logger.LogInformation(
            "Unit table: {UnitCount} units from {RowCount} table rows, {AvailableCount} available",
            units.Count,
            table.Rows.Count,
            units.Count(unit => unit.Status is UnitStatus.Available or UnitStatus.Unknown));
        return units;
    }
}
