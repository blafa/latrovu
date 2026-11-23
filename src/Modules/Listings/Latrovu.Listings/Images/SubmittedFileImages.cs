using Latrovu.BuildingBlocks.Images;
using Latrovu.Core.Submissions;

namespace Latrovu.Listings.Images;

public static class SubmittedFileImages
{
    private const string JpegContentType = "image/jpeg";
    private const string JpegExtension = ".jpg";
    private const string PngContentType = "image/png";
    private const string PngExtension = ".png";

    public static ImageFile ToImageFile(SubmittedFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (!file.IsImage)
        {
            throw new InvalidOperationException($"File {file.FileName} is not a supported image");
        }

        return file.IsPng
            ? new ImageFile(file.Content, PngContentType, PngExtension)
            : new ImageFile(file.Content, JpegContentType, JpegExtension);
    }
}
