namespace Latrovu.Core.Submissions;

public sealed record SubmittedFile(string FileName, string MimeType, byte[] Content)
{
    private const string PdfMimeType = "application/pdf";
    private const string PdfExtension = ".pdf";
    private const string JpegMimeType = "image/jpeg";
    private const string PngMimeType = "image/png";

    public bool IsPdf =>
        MimeType.Equals(PdfMimeType, StringComparison.OrdinalIgnoreCase)
        || FileName.EndsWith(PdfExtension, StringComparison.OrdinalIgnoreCase);

    public bool IsJpeg => MimeType.Equals(JpegMimeType, StringComparison.OrdinalIgnoreCase);

    public bool IsPng => MimeType.Equals(PngMimeType, StringComparison.OrdinalIgnoreCase);

    public bool IsImage => IsJpeg || IsPng;
}
