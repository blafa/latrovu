using Latrovu.BuildingBlocks.Pdf;
using System.Security.Cryptography;
using Latrovu.BuildingBlocks.Images;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Latrovu.BuildingBlocks.Infrastructure.Pdf;

public sealed class PdfPigImageReader(ILogger<PdfPigImageReader> logger) : IPdfImageReader
{
    public const int MinDimensionPixels = 300;
    public const int MaxImages = 30;
    private const string PngContentType = "image/png";
    private const string PngExtension = ".png";
    private const string JpegContentType = "image/jpeg";
    private const string JpegExtension = ".jpg";
    private const byte JpegMarkerFirst = 0xFF;
    private const byte JpegMarkerSecond = 0xD8;

    public IReadOnlyList<ImageFile> ReadImages(byte[] pdf)
    {
        ArgumentNullException.ThrowIfNull(pdf);

        using PdfDocument document = PdfDocument.Open(pdf);
        List<ImageFile> images = [];
        HashSet<string> seenHashes = [];
        foreach (Page page in document.GetPages())
        {
            AddPageImages(page, images, seenHashes);
            if (images.Count >= MaxImages)
            {
                break;
            }
        }

        logger.LogDebug("Extracted {ImageCount} images from PDF", images.Count);
        return images;
    }

    private void AddPageImages(Page page, List<ImageFile> images, HashSet<string> seenHashes)
    {
        foreach (IPdfImage image in page.GetImages())
        {
            ImageFile? file = TryConvert(image);
            if (file is not null && images.Count < MaxImages && seenHashes.Add(Convert.ToHexString(SHA256.HashData(file.Content))))
            {
                images.Add(file);
            }
        }
    }

    private ImageFile? TryConvert(IPdfImage image)
    {
        if (image.WidthInSamples < MinDimensionPixels || image.HeightInSamples < MinDimensionPixels)
        {
            return null;
        }

        try
        {
            return ToImageFile(image);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Skipped an image that could not be converted");
            return null;
        }
    }

    private static ImageFile? ToImageFile(IPdfImage image)
    {
        byte[] raw = image.RawBytes.ToArray();
        if (raw.Length > 1 && raw[0] == JpegMarkerFirst && raw[1] == JpegMarkerSecond)
        {
            return new ImageFile(raw, JpegContentType, JpegExtension);
        }

        return image.TryGetPng(out byte[] png) ? new ImageFile(png, PngContentType, PngExtension) : null;
    }
}
