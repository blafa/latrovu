using Latrovu.Api.Images;

namespace Latrovu.Api.Mail;

public sealed record MailAttachment(string FileName, string MimeType, byte[] Content)
{
    private const string PdfMimeType = "application/pdf";
    private const string PdfExtension = ".pdf";
    private const string JpegMimeType = "image/jpeg";
    private const string JpegExtension = ".jpg";
    private const string PngMimeType = "image/png";
    private const string PngExtension = ".png";

    public bool IsPdf =>
        MimeType.Equals(PdfMimeType, StringComparison.OrdinalIgnoreCase)
        || FileName.EndsWith(PdfExtension, StringComparison.OrdinalIgnoreCase);

    public bool IsImage => IsJpeg || IsPng;

    private bool IsJpeg => MimeType.Equals(JpegMimeType, StringComparison.OrdinalIgnoreCase);

    private bool IsPng => MimeType.Equals(PngMimeType, StringComparison.OrdinalIgnoreCase);

    public ImageFile ToImageFile()
    {
        if (!IsImage)
        {
            throw new InvalidOperationException($"Attachment {FileName} is not a supported image");
        }

        return IsPng
            ? new ImageFile(Content, PngMimeType, PngExtension)
            : new ImageFile(Content, JpegMimeType, JpegExtension);
    }
}
