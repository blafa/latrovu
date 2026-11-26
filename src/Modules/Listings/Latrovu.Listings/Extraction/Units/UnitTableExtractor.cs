using Latrovu.BuildingBlocks.Ai;

namespace Latrovu.Listings.Extraction.Units;

public sealed class UnitTableExtractor(ILlmClient llm, ILogger<UnitTableExtractor> logger) : IUnitTableExtractor
{
    private const char PageSeparator = '\n';

    public async Task<IReadOnlyList<ListingUnit>> ExtractAsync(IReadOnlyList<string> pages, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pages);

        string documentText = string.Join(PageSeparator, pages);
        if (string.IsNullOrWhiteSpace(documentText))
        {
            return [];
        }

        string? answer = await llm.CompleteAsync(UnitTablePrompt.Text, documentText, UnitTableSchema.Value, ct);
        IReadOnlyList<ListingUnit> units = UnitTableParser.Parse(LlmJsonNormalizer.Normalize(answer));
        logger.LogInformation(
            "Unit table: {UnitCount} units, {AvailableCount} available",
            units.Count,
            units.Count(unit => unit.Status is UnitStatus.Available or UnitStatus.Unknown));
        return units;
    }
}
