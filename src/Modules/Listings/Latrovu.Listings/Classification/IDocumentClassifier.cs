namespace Latrovu.Listings.Classification;

public interface IDocumentClassifier
{
    Task<DocumentClassification> ClassifyAsync(string documentText, CancellationToken ct);
}
