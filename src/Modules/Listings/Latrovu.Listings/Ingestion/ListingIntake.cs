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
            (IntakeFileOutcome outcome, IReadOnlyList<Guid> listingIds) = await ProcessPdfAsync(message, pdf, ct);
            outcomes.Add(outcome);
            createdListings.AddRange(listingIds.Select(listingId => new CreatedListing(listingId, pdf.Content)));
        }

        await imageCollector.CollectAsync(message, files, createdListings, ct);
        await replies.SendAsync(new IntakeReport(message.Sender, message.Subject, outcomes), ct);
    }

    private async Task<(IntakeFileOutcome Outcome, IReadOnlyList<Guid> ListingIds)> ProcessPdfAsync(Submission message, SubmittedFile pdf, CancellationToken ct)
    {
        IReadOnlyList<string> pages = pdfReader.ReadPages(pdf.Content);
        string sourceHash = Convert.ToHexString(SHA256.HashData(pdf.Content));
        if (await store.FindActiveBySourceAsync(message.Sender, sourceHash, ct) is { } existingId)
        {
            string reviewToken = await ReuseExistingAsync(existingId, pdf, pages, ct);
            return (new IntakeFileOutcome(pdf.FileName, IntakeFileStatus.AlreadyKnown, reviewToken, ListingId: existingId), []);
        }

        DocumentClassification classification = await classifier.ClassifyAsync(string.Join(PageSeparator, pages), ct);
        if (!classification.IsListing)
        {
            logger.LogInformation("Skipped {FileName}, it is a {DocumentType}", pdf.FileName, classification.Type);
            return (new IntakeFileOutcome(pdf.FileName, IntakeFileStatus.NotAListing, DocumentType: classification.Type), []);
        }

        IReadOnlyList<ListingUnit> units = classification.Type == DocumentType.MultiUnitListing ? await unitExtractor.ExtractAsync(pages, ct) : [];
        if (units.Count > 0)
        {
            return await StoreProjectAsync(message, pdf, pages, sourceHash, units, ct);
        }

        (Guid listingId, string token) = await ExtractAndStoreAsync(message, pdf, pages, sourceHash, ct);
        return (new IntakeFileOutcome(pdf.FileName, IntakeFileStatus.Created, token, ListingId: listingId), [listingId]);
    }

    private async Task<(IntakeFileOutcome Outcome, IReadOnlyList<Guid> ListingIds)> StoreProjectAsync(
        Submission message,
        SubmittedFile pdf,
        IReadOnlyList<string> pages,
        string sourceHash,
        IReadOnlyList<ListingUnit> units,
        CancellationToken ct)
    {
        ListingExtraction? building = await extractor.ExtractAsync(pages, ct);
        List<ListingUnit> available = units.Where(unit => unit.Status != UnitStatus.Rented).ToList();
        string title = ProjectTitles.From(building, pdf.FileName);
        Guid projectId = await store.AddProjectAsync(new NewProject(message.Sender, pdf.FileName, sourceHash, title, units.Count, units.Count - available.Count), ct);
        ProjectIntakeContext context = new(message, pdf, pages, sourceHash, projectId, building);
        List<IntakeUnitOutcome> stored = [];
        foreach (ListingUnit unit in available)
        {
            stored.Add(await StoreUnitAsync(context, unit, ct));
        }

        logger.LogInformation("Stored project {ProjectId} from {FileName} with {StoredCount} of {UnitCount} units", projectId, pdf.FileName, stored.Count, units.Count);
        IntakeProjectOutcome project = new(title, units.Count - available.Count, stored);
        return (new IntakeFileOutcome(pdf.FileName, IntakeFileStatus.Created, stored.FirstOrDefault()?.ReviewToken, ListingId: stored.FirstOrDefault()?.ListingId, Project: project),
            stored.Select(unit => unit.ListingId).ToList());
    }

    private async Task<IntakeUnitOutcome> StoreUnitAsync(ProjectIntakeContext context, ListingUnit unit, CancellationToken ct)
    {
        ListingExtraction? extraction = context.Building is null ? null : UnitListingComposer.Compose(context.Building, unit);
        NewListing listing = new(
            context.Message.Sender,
            context.Message.Subject,
            context.Pdf.FileName,
            extraction,
            context.Pages,
            context.SourceHash,
            context.ProjectId,
            unit.Label);
        Guid listingId = await store.AddAsync(listing, ct);
        string token = await accessTokens.CreateAsync(listingId, ct);
        return new IntakeUnitOutcome(unit.Label, unit.Rooms, unit.GrossRentChf, token, listingId);
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
