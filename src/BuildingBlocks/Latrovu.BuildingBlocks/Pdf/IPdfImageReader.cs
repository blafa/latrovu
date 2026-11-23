using Latrovu.BuildingBlocks.Images;

namespace Latrovu.BuildingBlocks.Pdf;

public interface IPdfImageReader
{
    IReadOnlyList<ImageFile> ReadImages(byte[] pdf);
}
