using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Latrovu.BuildingBlocks.Ai;
using Latrovu.Listings.Viewing.Chat;

namespace Latrovu.Listings.Infrastructure.Viewing.Chat;

public sealed class ListingChatAnswerer(ILlmClient llm, ILogger<ListingChatAnswerer> logger) : IListingChatAnswerer
{
    private const int HistoryMessages = 6;
    private const int MaxAnswerTokens = 400;
    private const string RequestKind = "request";
    private const string ContactKind = "contact";
    private const string BookKind = "book";
    private const string ViewingKind = "viewing";

    private const string SystemPrompt = """
        You answer questions from a person interested in one property listing in Switzerland, on behalf of Latrovu.
        You receive the listing data as JSON, earlier answers of the provider, and the text of the agent's exposé.

        First decide the kind of message:
        - "viewing": the person asks when viewings are possible, or wants to visit or see the property, without picking
          one specific listed slot. Write one short sentence in their language listing the free viewing slots from the
          provider's earlier answers by day and time; leave answer empty if none are listed.
        - "request": the person asks us to do something else: contact the provider, pass on a message, an offer or
          documents, call back.
        - "book": the person clearly picks one of the listed free viewing slots (by day, time or number). Set slot to
          that slot's number (the digits after #) and write one short sentence confirming that the viewing at that time
          is booked and the provider is informed. If it is unclear which slot is meant, use "question" instead.
        - "contact": the person asks for names, phone numbers, email addresses or other contact details.
        - "question": everything else.

        For "question":
        - Answer only from the provider's earlier answers, the listing data and the exposé text. Never invent or guess.
          Prefer the provider's earlier answers when they cover the question.
        - If the answer is not clearly there, set source to "unknown" and write one short sentence saying that this is
          not mentioned in the exposé.
        - Never name the provider, the agency, a company or a person; say "the provider". Never tell the person to
          contact anyone directly.
        - No legal, tax or financial advice.
        - source: "viewings" (the answer lists viewing slots; name them by day and time, never by number), "provider"
          (from an earlier answer of the provider), "expose" (then page = page number), "data", or "unknown".
        For "request" and "contact" leave answer empty, source "unknown", page null. slot is null unless kind is "book".

        language: ISO 639-1 code of the message language. Answer in that language, briefly (one to three sentences),
        formally (Sie, vous, Lei).
        """;

    private static readonly JsonResponseSchema Schema = new("listing_chat_reply", JsonSchemaSealer.Seal("""
        {
          "type": "object",
          "properties": {
            "kind": { "enum": ["viewing", "book", "request", "contact", "question"] },
            "language": { "type": "string" },
            "answer": { "type": "string", "maxLength": 600 },
            "source": { "enum": ["viewings", "provider", "expose", "data", "unknown"] },
            "page": { "type": ["integer", "null"] },
            "slot": { "type": ["integer", "null"] }
          },
          "required": ["kind", "language", "answer", "source", "page", "slot"]
        }
        """), MaxAnswerTokens);

    public async Task<ChatAnswer?> AnswerAsync(
        string listingDataJson,
        IReadOnlyList<string> pages,
        IReadOnlyList<KnowledgeEntry> knowledge,
        IReadOnlyList<ChatMessage> history,
        string question,
        CancellationToken ct)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        string? reply = await llm.CompleteAsync(SystemPrompt, UserContent(listingDataJson, pages, knowledge, history, question), Schema, ct);
        logger.LogInformation("Chat answer generated in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
        return reply is null ? null : Parse(reply, pages.Count, ContactRedactor.FromListingData(listingDataJson));
    }

    private ChatAnswer? Parse(string reply, int pageCount, ContactRedactor redactor)
    {
        try
        {
            return ParseJson(reply, pageCount, redactor);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Chat answer was not valid JSON ({Length} characters), treated as unknown", reply.Length);
            return null;
        }
    }

    private static ChatAnswer? ParseJson(string reply, int pageCount, ContactRedactor redactor)
    {
        if (JsonNode.Parse(reply) is not JsonObject root)
        {
            return null;
        }

        string language = root["language"]?.GetValue<string>() ?? string.Empty;
        return root["kind"]?.GetValue<string>() switch
        {
            RequestKind => new ChatAnswer(ChatReplyTexts.RequestForwarded(language), ChatMessage.RequestSource, null),
            ContactKind => new ChatAnswer(ChatReplyTexts.ContactViaLatrovu(language), ChatMessage.LatrovuSource, null),
            ViewingKind => new ChatAnswer(redactor.Redact(root["answer"]?.GetValue<string>()?.Trim() ?? string.Empty, ChatReplyTexts.Provider(language)), ChatMessage.ViewingsSource, null),
            BookKind when root["slot"]?.GetValue<long>() is { } slot => Answer(root, pageCount, redactor, language) is { } confirmation
                ? confirmation with { Source = ChatMessage.BookingSource, Page = null, SlotId = slot }
                : null,
            _ => Answer(root, pageCount, redactor, language)
        };
    }

    private static ChatAnswer? Answer(JsonObject root, int pageCount, ContactRedactor redactor, string language)
    {
        if (root["answer"]?.GetValue<string>() is not { Length: > 0 } text)
        {
            return null;
        }

        string source = root["source"]?.GetValue<string>() ?? ChatMessage.UnknownSource;
        int? page = root["page"]?.GetValue<int>();
        return new ChatAnswer(redactor.Redact(text.Trim(), ChatReplyTexts.Provider(language)), source, page is >= 1 && page <= pageCount ? page : null);
    }

    private static string UserContent(
        string listingDataJson,
        IReadOnlyList<string> pages,
        IReadOnlyList<KnowledgeEntry> knowledge,
        IReadOnlyList<ChatMessage> history,
        string question)
    {
        StringBuilder content = new($"Listing data:\n{listingDataJson}\n\nEarlier answers of the provider:\n");
        foreach (KnowledgeEntry entry in knowledge)
        {
            content.Append(CultureInfo.InvariantCulture, $"Q: {entry.Question}\nA: {entry.Answer}\n");
        }

        content.Append("\nExposé:\n");
        for (int index = 0; index < pages.Count; index++)
        {
            content.Append(CultureInfo.InvariantCulture, $"=== Page {index + 1} ===\n{pages[index]}\n");
        }

        content.Append("\nConversation so far:\n");
        foreach (ChatMessage message in history.TakeLast(HistoryMessages))
        {
            content.Append(CultureInfo.InvariantCulture, $"{message.Role}: {message.Text}\n");
        }

        return content.Append(CultureInfo.InvariantCulture, $"\nMessage:\n{question}").ToString();
    }
}
