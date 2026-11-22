using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Latrovu.Api.Pdf;

public sealed class PdfPigTextReader : IPdfTextReader
{
    public const int MaxCharacters = 40000;

    public string ReadText(byte[] pdf)
    {
        ArgumentNullException.ThrowIfNull(pdf);

        using PdfDocument document = PdfDocument.Open(pdf);
        StringBuilder text = new();
        foreach (Page page in document.GetPages())
        {
            text.AppendLine(page.Text);
            if (text.Length >= MaxCharacters)
            {
                break;
            }
        }

        return text.Length > MaxCharacters ? text.ToString(0, MaxCharacters) : text.ToString();
    }
}
