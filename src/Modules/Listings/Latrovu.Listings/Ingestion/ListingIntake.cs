using System.Security.Cryptography;
using Latrovu.BuildingBlocks.Pdf;
using Latrovu.Core.Submissions;
using Latrovu.Listings.Extraction.Units;

namespace Latrovu.Listings.Ingestion;

public sealed class ListingIntake(
    IPdfTextReader pdfReader,
    IDocumentClassifier classifier,
    IListingExtractor extractor,
    IUnitTableExtractor unitExtractor,
    IListingStore store,
    IListingAccessTokenStore accessTokens,
    IListingImageCollector imageCollector,
    IIntakeReplyNotifier replies,
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
            await replies.SendAsync(new IntakeReport(message.Sender, message.Subject, []), ct);
            return;
        }

        List<IntakeFileOutcome> outcomes = [];
        List<CreatedListing> createdListings = [];
        foreach (SubmittedFile pdf in pdfs)
        {
            (IntakeFileOutcome outcome, Guid? listingId) = await ProcessPdfAsync(message, pdf, ct);
            outcomes.Add(outcome);
            if (listingId is not null)
            {
                createdListings.Add(new CreatedListing(listingId.Value, pdf.Content));
            }
        }

        await imageCollector.CollectAsync(message, files, createdListings, ct);
        await replies.SendAsync(new IntakeReport(message.Sender, message.Subject, outcomes), ct);
    }

    private async Task<(IntakeFileOutcome Outcome, Guid? ListingId)> ProcessPdfAsync(Submission message, SubmittedFile pdf, CancellationToken ct)
    {
        IReadOnlyList<string> pages = pdfReader.ReadPages(pdf.Content);
        string sourceHash = Convert.ToHexString(SHA256.HashData(pdf.Content));
        if (await store.FindActiveBySourceAsync(message.Sender, sourceHash, ct) is { } existingId)
        {
            string reviewToken = await ReuseExistingAsync(existingId, pdf, pages, ct);
            return (new IntakeFileOutcome(pdf.FileName, IntakeFileStatus.AlreadyKnown, reviewToken, ListingId: existingId), null);
        }

        DocumentClassification classification = await classifier.ClassifyAsync(string.Join(PageSeparator, pages), ct);
        if (!classification.IsListing)
        {
            logger.LogInformation("Skipped {FileName}, it is a {DocumentType}", pdf.FileName, classification.Type);
            return (new IntakeFileOutcome(pdf.FileName, IntakeFileStatus.NotAListing, DocumentType: classification.Type), null);
        }

        if (classification.Type == DocumentType.MultiUnitListing)
        {
            await LogUnitsAsync(pdf, pages, ct);
        }

        (Guid listingId, string token) = await ExtractAndStoreAsync(message, pdf, pages, sourceHash, ct);
        return (new IntakeFileOutcome(pdf.FileName, IntakeFileStatus.Created, token, ListingId: listingId), listingId);
    }

    private async Task LogUnitsAsync(SubmittedFile pdf, IReadOnlyList<string> pages, CancellationToken ct)
    {
        IReadOnlyList<ListingUnit> units = await unitExtractor.ExtractAsync(pages, ct);
        logger.LogWarning("{FileName} contains {UnitCount} units, still stored as one listing until projects exist", pdf.FileName, units.Count);
        foreach (ListingUnit unit in units)
        {
            logger.LogInformation(
                "Unit {Label}: {Rooms} rooms, floor {Floor}, {Area} m2, net {Net}, charges {Charges}, gross {Gross}, from {From}, {Status}",
                unit.Label, unit.Rooms, unit.Floor, unit.LivingAreaM2, unit.NetRentChf, unit.ChargesChf, unit.GrossRentChf, unit.AvailableFrom, unit.Status);
        }
    }

    private async Task<string> ReuseExistingAsync(Guid listingId, SubmittedFile pdf, IReadOnlyList<string> pages, CancellationToken ct)
    {
        await store.FillMissingSourcePagesAsync(listingId, pages, ct);
        string token = await accessTokens.CreateAsync(listingId, ct);
        logger.LogInformation("{FileName} is already listing {ListingId}, no copy created, new review link issued", pdf.FileName, listingId);
        return token;
    }

    private async Task<(Guid ListingId, string Token)> ExtractAndStoreAsync(
        Submission message,
        SubmittedFile pdf,
        IReadOnlyList<string> pages,
        string sourceHash,
        CancellationToken ct)
    {
        ListingExtraction? extraction = await extractor.ExtractAsync(pages, ct);
        NewListing listing = new(message.Sender, message.Subject, pdf.FileName, extraction, pages, sourceHash);
        Guid listingId = await store.AddAsync(listing, ct);
        string token = await accessTokens.CreateAsync(listingId, ct);
        logger.LogInformation("Stored listing {ListingId} from {FileName}, review link issued", listingId, pdf.FileName);
        return (listingId, token);
    }
}
