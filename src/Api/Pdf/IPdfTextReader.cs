namespace Latrovu.Api.Pdf;

public interface IPdfTextReader
{
    string ReadText(byte[] pdf);
}
