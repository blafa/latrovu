using Latrovu.BuildingBlocks.Ai;

namespace Latrovu.Listings.Extraction;

public sealed class ListingExtractor(ILlmClient llm, ILogger<ListingExtractor> logger) : IListingExtractor
{
    private const char PageSeparator = '\n';

    public async Task<ListingExtraction?> ExtractAsync(IReadOnlyList<string> pages, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(pages);

        string exposeText = string.Join(PageSeparator, pages);
        if (string.IsNullOrWhiteSpace(exposeText))
        {
            logger.LogWarning("Exposé contains no readable text, probably a scanned document");
            return null;
        }

        string? answer = await llm.CompleteAsync(ListingExtractionPrompt.Text, exposeText, ListingSchema.Value, ct);
        string? json = LlmJsonNormalizer.Normalize(answer);
        if (json is null)
        {
            logger.LogWarning("LLM returned no answer for the exposé");
            return null;
        }

        if (json.Contains(LlmJsonNormalizer.RawAnswerProperty, StringComparison.Ordinal))
        {
            logger.LogWarning("LLM answer was not valid JSON, keeping the raw answer");
        }

        return ListingExtractionBuilder.Build(json, pages);
    }
}
