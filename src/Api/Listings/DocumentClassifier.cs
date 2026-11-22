using Latrovu.Api.Ai;

namespace Latrovu.Api.Listings;

public sealed class DocumentClassifier(ILlmClient llm, ILogger<DocumentClassifier> logger) : IDocumentClassifier
{
    public const int SampleCharacters = 6000;
    private const int LoggedAnswerCharacters = 300;

    public async Task<DocumentClassification> ClassifyAsync(string documentText, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(documentText))
        {
            return new DocumentClassification(DocumentType.Other, "Document contains no readable text");
        }

        string? answer = await RequestClassificationAsync(documentText, ct);
        DocumentClassification? classification = DocumentClassificationParser.Parse(answer);
        if (classification is null)
        {
            logger.LogWarning(
                "Classification answer could not be read, treating document as listing. Answer: {Answer}",
                Shorten(answer));
            return DocumentClassificationParser.FallbackToListing();
        }

        logger.LogInformation("Document classified as {DocumentType}: {Reason}", classification.Type, classification.Reason);
        return classification;
    }

    private Task<string?> RequestClassificationAsync(string documentText, CancellationToken ct)
    {
        string sample = documentText.Length > SampleCharacters ? documentText[..SampleCharacters] : documentText;
        return llm.CompleteAsync(DocumentClassificationPrompt.Text, sample, DocumentClassificationSchema.Value, ct);
    }

    private static string Shorten(string? answer)
    {
        if (answer is null)
        {
            return string.Empty;
        }

        return answer.Length > LoggedAnswerCharacters ? answer[..LoggedAnswerCharacters] : answer;
    }
}
