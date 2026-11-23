using Latrovu.BuildingBlocks.Pdf;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Latrovu.BuildingBlocks.Infrastructure.Pdf;

public sealed class PdfPigTextReader : IPdfTextReader
{
    public const int MaxCharacters = 40000;

    public IReadOnlyList<string> ReadPages(byte[] pdf)
    {
        ArgumentNullException.ThrowIfNull(pdf);

        using PdfDocument document = PdfDocument.Open(pdf);
        List<string> pages = [];
        int remaining = MaxCharacters;
        foreach (Page page in document.GetPages())
        {
            string text = page.Text.Length > remaining ? page.Text[..remaining] : page.Text;
            pages.Add(text);
            remaining -= text.Length;
            if (remaining <= 0)
            {
                break;
            }
        }

        return pages;
    }
}
