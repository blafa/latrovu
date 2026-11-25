using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Latrovu.BuildingBlocks.Ai;
using Latrovu.Listings.Viewing.Chat;

namespace Latrovu.Listings.Infrastructure.Viewing.Chat;

public sealed class LlmAnswerTranslator(ILlmClient llm, ILogger<LlmAnswerTranslator> logger) : IAnswerTranslator
{
    private const int MaxAnswerTokens = 800;
    private const string FallbackProvider = "der Anbieter";

    private const string SystemPrompt = """
        A property provider answered questions from an interested person. Prepare the answer for that person.
        - Write it in the language of the person's questions. If the answer already is in that language, keep the
          wording as it is.
        - Remove greetings, closing formulas and signatures.
        - Remove names of persons and companies, phone numbers, email addresses and postal addresses of the provider.
        - Keep every fact exactly as the provider wrote it. Add nothing.
        - language: ISO 639-1 code of the questions. translated: true if you changed the language.
        - general: true if the answer is a fact about the property that is useful for every interested person (pets,
          parking, heating costs, renovations). false if it concerns this person only (their appointment, their
          situation, their offer).
        """;

    private static readonly JsonResponseSchema Schema = new("seeker_answer", JsonSchemaSealer.Seal("""
        {
          "type": "object",
          "properties": {
            "language": { "type": "string" },
            "translated": { "type": "boolean" },
            "general": { "type": "boolean" },
            "answer": { "type": "string", "maxLength": 2000 }
          },
          "required": ["language", "translated", "general", "answer"]
        }
        """), MaxAnswerTokens);

    public async Task<TranslatedAnswer> PrepareForSeekerAsync(
        string answer,
        IReadOnlyList<string> questions,
        string listingDataJson,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(questions);

        ContactRedactor redactor = ContactRedactor.FromListingData(listingDataJson);
        Stopwatch stopwatch = Stopwatch.StartNew();
        string? reply = await llm.CompleteAsync(SystemPrompt, $"Questions:\n- {string.Join("\n- ", questions)}\n\nAnswer:\n{answer}", Schema, ct);
        logger.LogInformation("Agent answer prepared for seeker in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
        TranslatedAnswer prepared = Parse(reply) ?? new TranslatedAnswer(answer, false);
        return prepared with { Text = redactor.Redact(prepared.Text, FallbackProvider) };
    }

    private TranslatedAnswer? Parse(string? reply)
    {
        try
        {
            return reply is not null && JsonNode.Parse(reply) is JsonObject root && root["answer"]?.GetValue<string>() is { Length: > 0 } text
                ? new TranslatedAnswer(text.Trim(), root["translated"]?.GetValue<bool>() ?? false, root["general"]?.GetValue<bool>() ?? false)
                : null;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Prepared answer was not valid JSON, original text used");
            return null;
        }
    }
}
