using Latrovu.Api.Images;

namespace Latrovu.Api.Pdf;

public interface IPdfImageReader
{
    IReadOnlyList<ImageFile> ReadImages(byte[] pdf);
}
