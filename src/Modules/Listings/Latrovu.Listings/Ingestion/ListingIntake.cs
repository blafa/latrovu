using Latrovu.BuildingBlocks.Pdf;
using Latrovu.Core.Submissions;

namespace Latrovu.Listings.Ingestion;

public sealed class ListingIntake(
    IPdfTextReader pdfReader,
    IDocumentClassifier classifier,
    IListingExtractor extractor,
    IListingStore store,
    IListingAccessTokenStore accessTokens,
    IListingImageCollector imageCollector,
    ILogger<ListingIntake> logger) : IListingIntake
{
    private const char PageSeparator = '\n';

    public async Task HandleAsync(Submission message, IReadOnlyList<SubmittedFile> files, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(files);

        List<SubmittedFile> pdfs = files.Where(file => file.IsPdf).ToList();
        if (pdfs.Count == 0)
        {
            logger.LogWarning("Listing message {MessageId} has no PDF attachment", message.MessageId);
            return;
        }

        List<CreatedListing> createdListings = await CreateListingsAsync(message, pdfs, ct);
        await imageCollector.CollectAsync(message, files, createdListings, ct);
    }

    private async Task<List<CreatedListing>> CreateListingsAsync(Submission message, List<SubmittedFile> pdfs, CancellationToken ct)
    {
        List<CreatedListing> createdListings = [];
        foreach (SubmittedFile pdf in pdfs)
        {
            Guid? listingId = await ProcessPdfAsync(message, pdf, ct);
            if (listingId is not null)
            {
                createdListings.Add(new CreatedListing(listingId.Value, pdf.Content));
            }
        }

        return createdListings;
    }

    private async Task<Guid?> ProcessPdfAsync(Submission message, SubmittedFile pdf, CancellationToken ct)
    {
        IReadOnlyList<string> pages = pdfReader.ReadPages(pdf.Content);
        DocumentClassification classification = await classifier.ClassifyAsync(string.Join(PageSeparator, pages), ct);
        if (!classification.IsListing)
        {
            logger.LogInformation("Skipped {FileName}, it is a {DocumentType}", pdf.FileName, classification.Type);
            return null;
        }

        if (classification.Type == DocumentType.MultiUnitListing)
        {
            logger.LogWarning("{FileName} contains several units, extracting it as one listing for now", pdf.FileName);
        }

        return await ExtractAndStoreAsync(message, pdf, pages, ct);
    }

    private async Task<Guid> ExtractAndStoreAsync(Submission message, SubmittedFile pdf, IReadOnlyList<string> pages, CancellationToken ct)
    {
        ListingExtraction? extraction = await extractor.ExtractAsync(pages, ct);
        NewListing listing = new(message.Sender, message.Subject, pdf.FileName, extraction, pages);
        Guid listingId = await store.AddAsync(listing, ct);
        string token = await accessTokens.CreateAsync(listingId, ct);
        logger.LogInformation("Stored listing {ListingId} from {FileName}, review at /review/{Token}", listingId, pdf.FileName, token);
        return listingId;
    }
}
