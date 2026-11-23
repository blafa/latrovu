using Latrovu.BuildingBlocks.Pdf;
using Latrovu.Core.Messaging;

namespace Latrovu.Listings;

public sealed class ListingIntake(
    IPdfTextReader pdfReader,
    IDocumentClassifier classifier,
    IListingExtractor extractor,
    IListingStore store,
    IListingImageCollector imageCollector,
    ILogger<ListingIntake> logger) : IListingDocumentHandler
{
    public async Task HandleAsync(ReceivedMessage message, IReadOnlyList<ReceivedFile> files, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(files);

        List<ReceivedFile> pdfs = files.Where(file => file.IsPdf).ToList();
        if (pdfs.Count == 0)
        {
            logger.LogWarning("Listing message {MessageId} has no PDF attachment", message.MessageId);
            return;
        }

        List<CreatedListing> createdListings = await CreateListingsAsync(message, pdfs, ct);
        await imageCollector.CollectAsync(message, files, createdListings, ct);
    }

    private async Task<List<CreatedListing>> CreateListingsAsync(ReceivedMessage message, List<ReceivedFile> pdfs, CancellationToken ct)
    {
        List<CreatedListing> createdListings = [];
        foreach (ReceivedFile pdf in pdfs)
        {
            Guid? listingId = await ProcessPdfAsync(message, pdf, ct);
            if (listingId is not null)
            {
                createdListings.Add(new CreatedListing(listingId.Value, pdf.Content));
            }
        }

        return createdListings;
    }

    private async Task<Guid?> ProcessPdfAsync(ReceivedMessage message, ReceivedFile pdf, CancellationToken ct)
    {
        string text = pdfReader.ReadText(pdf.Content);
        DocumentClassification classification = await classifier.ClassifyAsync(text, ct);
        if (!classification.IsListing)
        {
            logger.LogInformation("Skipped {FileName}, it is a {DocumentType}", pdf.FileName, classification.Type);
            return null;
        }

        if (classification.Type == DocumentType.MultiUnitListing)
        {
            logger.LogWarning("{FileName} contains several units, extracting it as one listing for now", pdf.FileName);
        }

        return await ExtractAndStoreAsync(message, pdf, text, ct);
    }

    private async Task<Guid> ExtractAndStoreAsync(ReceivedMessage message, ReceivedFile pdf, string text, CancellationToken ct)
    {
        ListingExtraction? extraction = await extractor.ExtractAsync(text, ct);
        NewListing listing = new(message.Sender, message.Subject, pdf.FileName, extraction);
        Guid listingId = await store.AddAsync(listing, ct);
        logger.LogInformation("Stored listing {ListingId} from {FileName}", listingId, pdf.FileName);
        return listingId;
    }
}
