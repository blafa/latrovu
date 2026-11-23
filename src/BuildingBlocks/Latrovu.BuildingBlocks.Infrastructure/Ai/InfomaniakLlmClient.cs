using Latrovu.BuildingBlocks.Ai;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace Latrovu.BuildingBlocks.Infrastructure.Ai;

public sealed class InfomaniakLlmClient(
    HttpClient http,
    IOptions<AiOptions> options,
    ILogger<InfomaniakLlmClient> logger) : ILlmClient
{
    private const double DeterministicTemperature = 0;
    private const string SystemRole = "system";
    private const string UserRole = "user";
    private const string JsonSchemaFormat = "json_schema";

    private readonly AiOptions settings = options.Value;

    public async Task<string?> CompleteAsync(
        string systemPrompt,
        string userContent,
        JsonResponseSchema responseSchema,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(responseSchema);

        Stopwatch stopwatch = Stopwatch.StartNew();
        using HttpResponseMessage response = await http.PostAsJsonAsync(
            CompletionsPath(),
            BuildRequest(systemPrompt, userContent, responseSchema),
            ct);
        string body = await response.Content.ReadAsStringAsync(ct);
        logger.LogInformation(
            "LLM {Model} answered {StatusCode} in {ElapsedMs} ms",
            settings.Model,
            (int)response.StatusCode,
            stopwatch.ElapsedMilliseconds);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("LLM call failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new HttpRequestException($"LLM call failed with {(int)response.StatusCode}", null, response.StatusCode);
        }

        return ReadContent(body);
    }

    private string CompletionsPath() =>
        string.Format(CultureInfo.InvariantCulture, settings.CompletionsPathTemplate, settings.ProductId);

    private object BuildRequest(string systemPrompt, string userContent, JsonResponseSchema responseSchema) => new
    {
        model = settings.Model,
        temperature = DeterministicTemperature,
        max_tokens = settings.MaxOutputTokens,
        response_format = new
        {
            type = JsonSchemaFormat,
            json_schema = new
            {
                name = responseSchema.Name,
                strict = false,
                schema = JsonNode.Parse(responseSchema.Schema)
            }
        },
        messages = new object[]
        {
            new { role = SystemRole, content = systemPrompt },
            new { role = UserRole, content = userContent }
        }
    };

    private static string? ReadContent(string body)
    {
        using JsonDocument document = JsonDocument.Parse(body);
        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
    }
}
