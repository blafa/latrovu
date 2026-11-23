using Latrovu.BuildingBlocks.Images;
using Latrovu.Core.Messaging;

namespace Latrovu.Listings;

public static class ReceivedFileImages
{
    private const string JpegContentType = "image/jpeg";
    private const string JpegExtension = ".jpg";
    private const string PngContentType = "image/png";
    private const string PngExtension = ".png";

    public static ImageFile ToImageFile(ReceivedFile file)
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
