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
            string pageText = TextOf(page);
            string text = pageText.Length > remaining ? pageText[..remaining] : pageText;
            pages.Add(text);
            remaining -= text.Length;
            if (remaining <= 0)
            {
                break;
            }
        }

        return pages;
    }

    private static string TextOf(Page page)
    {
        List<PositionedWord> words = page.GetWords()
            .Select(word => new PositionedWord(word.Text, word.BoundingBox.Left, word.BoundingBox.Bottom, word.BoundingBox.Height))
            .ToList();
        return words.Count == 0 ? page.Text : LineAssembler.Assemble(words);
    }
}
