using Latrovu.BuildingBlocks.Ai;

namespace Latrovu.Listings;

public sealed class ListingExtractor(ILlmClient llm, ILogger<ListingExtractor> logger) : IListingExtractor
{
    public async Task<string?> ExtractAsync(string exposeText, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(exposeText))
        {
            logger.LogWarning("Exposé contains no readable text, probably a scanned document");
            return null;
        }

        string? answer = await llm.CompleteAsync(ListingExtractionPrompt.Text, exposeText, ListingSchema.Value, ct);
        string? json = LlmJsonNormalizer.Normalize(answer);
        if (json?.Contains(LlmJsonNormalizer.RawAnswerProperty, StringComparison.Ordinal) == true)
        {
            logger.LogWarning("LLM answer was not valid JSON, keeping the raw answer");
        }

        return json;
    }
}
