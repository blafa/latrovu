using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Latrovu.BuildingBlocks.Ai;

namespace Latrovu.Searches.Infrastructure;

public sealed class SearchInterpreter(ILlmClient llm, ILogger<SearchInterpreter> logger) : ISearchInterpreter
{
    private const string FallbackLanguage = "en";
    private const int LanguageCodeLength = 2;

    private const string SystemPrompt = """
        You process messages from people looking for a flat or house in Switzerland. Messages can be in any language,
        may be short, informal or contain spelling mistakes. Many writers are not used to computers: be generous.

        You receive the message and the person's existing searches as JSON. Determine:
        - language: ISO 639-1 code of the message language (e.g. de, fr, it, en, pt, sq, tr, ar, uk).
        - action: "new" for a new search, "update" to change an existing one, "pause", "resume", "stop",
          or "unclear" if the message contains no recognisable search wish.
        - searchName: short name after the main place, e.g. "Albisrieden" or "Zürich". For update, pause, resume
          and stop use the exact name of the affected existing search.
        - criteria: the complete criteria after this message. For update: the existing criteria with the changes.
          offerType rent or sale (null if not said), places as written by the person, prices in CHF per month for
          rent and total for sale, availableFrom as ISO date or a short phrase.
        - summary: one to three short, simple sentences in the language of the message saying what we understood.
          Address the person formally where the language has a formal form (Sie, vous, Lei, usted).
        - refinements: up to three short suggestions in the language of the message for missing details that would
          improve the search, e.g. budget or moving date, phrased as a polite question in the formal address.
          Empty if nothing important is missing.
        Never invent criteria the person did not mention.
        """;

    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    public async Task<SearchInterpretation?> InterpretAsync(string text, IReadOnlyList<Search> existingSearches, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(existingSearches);

        Stopwatch stopwatch = Stopwatch.StartNew();
        string? answer = await llm.CompleteAsync(SystemPrompt, UserContent(text, existingSearches), SearchInterpretationSchema.Value, ct);
        logger.LogInformation("Search interpretation took {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
        return answer is null ? null : Parse(answer);
    }

    private static string UserContent(string text, IReadOnlyList<Search> existingSearches)
    {
        JsonArray existing = new(existingSearches
            .Where(search => search.IsOpen)
            .Select(search => (JsonNode)new JsonObject
            {
                ["name"] = search.Name,
                ["status"] = search.Status,
                ["criteria"] = JsonNode.Parse(search.CriteriaJson)
            })
            .ToArray());
        return $"Existing searches:\n{existing.ToJsonString(WebJson)}\n\nMessage:\n{text}";
    }

    private SearchInterpretation? Parse(string answer)
    {
        if (JsonNode.Parse(answer) is not JsonObject root || !Enum.TryParse(root["action"]?.GetValue<string>(), true, out SearchAction action))
        {
            logger.LogWarning("Search interpretation could not be parsed");
            return null;
        }

        return new SearchInterpretation(
            NormalizeLanguage(root["language"]?.GetValue<string>()),
            action,
            root["searchName"]?.GetValue<string>(),
            root["criteria"]?.ToJsonString() ?? "{}",
            root["summary"]?.GetValue<string>() ?? string.Empty,
            root["refinements"]?.AsArray().Select(item => item?.GetValue<string>()).OfType<string>().ToList() ?? []);
    }

    private static string NormalizeLanguage(string? code) =>
        code is { Length: >= LanguageCodeLength } ? code[..LanguageCodeLength].ToLowerInvariant() : FallbackLanguage;
}
