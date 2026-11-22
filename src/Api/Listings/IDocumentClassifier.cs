namespace Latrovu.Api.Listings;

public interface IDocumentClassifier
{
    Task<DocumentClassification> ClassifyAsync(string documentText, CancellationToken ct);
}
