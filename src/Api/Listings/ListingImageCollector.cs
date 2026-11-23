using Latrovu.Api.Images;
using Latrovu.Api.Mail;
using Latrovu.Api.Pdf;

namespace Latrovu.Api.Listings;

public sealed class ListingImageCollector(
    IPdfImageReader pdfImageReader,
    IListingImageService imageService,
    ILogger<ListingImageCollector> logger) : IListingImageCollector
{
    public async Task CollectAsync(InboundMail mail, IReadOnlyList<CreatedListing> createdListings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mail);
        ArgumentNullException.ThrowIfNull(createdListings);

        List<ImageFile> mailPhotos = AssignableMailPhotos(mail, createdListings.Count);
        foreach (CreatedListing listing in createdListings)
        {
            await TryAttachAsync(listing, mailPhotos, ct);
        }
    }

    private List<ImageFile> AssignableMailPhotos(InboundMail mail, int listingCount)
    {
        List<ImageFile> photos = mail.Attachments
            .Where(attachment => attachment.IsImage)
            .Select(attachment => attachment.ToImageFile())
            .ToList();

        if (photos.Count > 0 && listingCount != 1)
        {
            logger.LogWarning(
                "Mail {MessageId} has {PhotoCount} photos for {ListingCount} listings, photos are not assigned",
                mail.MessageId,
                photos.Count,
                listingCount);
            return [];
        }

        return photos;
    }

    private async Task TryAttachAsync(CreatedListing listing, List<ImageFile> mailPhotos, CancellationToken ct)
    {
        try
        {
            List<ImageFile> images = [.. pdfImageReader.ReadImages(listing.SourcePdf), .. mailPhotos];
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
