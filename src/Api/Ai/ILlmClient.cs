namespace Latrovu.Api.Ai;

public interface ILlmClient
{
    Task<string?> CompleteAsync(
        string systemPrompt,
        string userContent,
        JsonResponseSchema responseSchema,
        CancellationToken ct);
}
