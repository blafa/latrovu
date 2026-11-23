using Latrovu.BuildingBlocks.Images;
using Latrovu.BuildingBlocks.Pdf;
using Latrovu.Core.Submissions;

namespace Latrovu.Listings.Images;

public sealed class ListingImageCollector(
    IPdfImageReader pdfImageReader,
    IListingImageService imageService,
    ILogger<ListingImageCollector> logger) : IListingImageCollector
{
    public async Task CollectAsync(
        Submission message,
        IReadOnlyList<SubmittedFile> files,
        IReadOnlyList<CreatedListing> createdListings,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(createdListings);

        List<ImageFile> photos = AssignablePhotos(message, files, createdListings.Count);
        foreach (CreatedListing listing in createdListings)
        {
            await TryAttachAsync(listing, photos, ct);
        }
    }

    private List<ImageFile> AssignablePhotos(Submission message, IReadOnlyList<SubmittedFile> files, int listingCount)
    {
        List<ImageFile> photos = files
            .Where(file => file.IsImage)
            .Select(SubmittedFileImages.ToImageFile)
            .ToList();

        if (photos.Count > 0 && listingCount != 1)
        {
            logger.LogWarning(
                "Message {MessageId} has {PhotoCount} photos for {ListingCount} listings, photos are not assigned",
                message.MessageId,
                photos.Count,
                listingCount);
            return [];
        }

        return photos;
    }

    private async Task TryAttachAsync(CreatedListing listing, List<ImageFile> photos, CancellationToken ct)
    {
        try
        {
            List<ImageFile> images = [.. pdfImageReader.ReadImages(listing.SourcePdf), .. photos];
            if (images.Count > 0)
            {
                await imageService.AttachAsync(listing.Id, images, ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Images for listing {ListingId} could not be attached, listing is kept without images", listing.Id);
        }
    }
}
