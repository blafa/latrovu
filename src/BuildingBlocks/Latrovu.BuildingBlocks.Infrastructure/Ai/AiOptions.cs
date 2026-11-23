using System.ComponentModel.DataAnnotations;

namespace Latrovu.BuildingBlocks.Infrastructure.Ai;

public sealed class AiOptions
{
    public const string SectionName = "AI";
    public const string DefaultBaseUrl = "https://api.infomaniak.com";
    public const string DefaultModel = "mistralai/Mistral-Small-4-119B-2603";
    public const string DefaultCompletionsPathTemplate = "/2/ai/{0}/openai/v1/chat/completions";
    public const int DefaultMaxOutputTokens = 5000;
    public const int MinOutputTokens = 1;
    public const int MaxOutputTokensLimit = 5000;

    [Required]
    public string ProductId { get; init; } = string.Empty;

    [Required]
    public string Token { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = DefaultBaseUrl;

    public string Model { get; init; } = DefaultModel;

    public string CompletionsPathTemplate { get; init; } = DefaultCompletionsPathTemplate;

    [Range(MinOutputTokens, MaxOutputTokensLimit)]
    public int MaxOutputTokens { get; init; } = DefaultMaxOutputTokens;
}
