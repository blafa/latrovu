namespace Latrovu.BuildingBlocks.Pdf;

public interface IPdfTextReader
{
    IReadOnlyList<string> ReadPages(byte[] pdf);
}
