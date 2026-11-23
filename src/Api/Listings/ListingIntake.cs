using Latrovu.Api.Data;
using Latrovu.Api.Mail;
using Latrovu.Api.Pdf;

namespace Latrovu.Api.Listings;

public sealed class ListingIntake(
    IPdfTextReader pdfReader,
    IDocumentClassifier classifier,
    IListingExtractor extractor,
    IListingStore store,
    IListingImageCollector imageCollector,
    ILogger<ListingIntake> logger) : IListingIntake
{
    public async Task HandleAsync(InboundMail mail, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mail);

        List<MailAttachment> pdfs = mail.Attachments.Where(attachment => attachment.IsPdf).ToList();
        if (pdfs.Count == 0)
        {
            logger.LogWarning("Listing mail {MessageId} has no PDF attachment", mail.MessageId);
            return;
        }

        List<CreatedListing> createdListings = await CreateListingsAsync(mail, pdfs, ct);
        await imageCollector.CollectAsync(mail, createdListings, ct);
    }

    private async Task<List<CreatedListing>> CreateListingsAsync(InboundMail mail, List<MailAttachment> pdfs, CancellationToken ct)
    {
        List<CreatedListing> createdListings = [];
        foreach (MailAttachment pdf in pdfs)
        {
            Guid? listingId = await ProcessPdfAsync(mail, pdf, ct);
            if (listingId is not null)
            {
                createdListings.Add(new CreatedListing(listingId.Value, pdf.Content));
            }
        }

        return createdListings;
    }

    private async Task<Guid?> ProcessPdfAsync(InboundMail mail, MailAttachment pdf, CancellationToken ct)
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

        return await ExtractAndStoreAsync(mail, pdf, text, ct);
    }

    private async Task<Guid> ExtractAndStoreAsync(InboundMail mail, MailAttachment pdf, string text, CancellationToken ct)
    {
        string? data = await extractor.ExtractAsync(text, ct);
        NewListing listing = new(mail.Sender, mail.Subject, pdf.FileName, data);
        Guid listingId = await store.AddAsync(listing, ct);
        logger.LogInformation("Stored listing {ListingId} from {FileName}", listingId, pdf.FileName);
        return listingId;
    }
}
