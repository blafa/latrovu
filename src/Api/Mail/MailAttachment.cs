namespace Latrovu.Api.Mail;

public sealed record MailAttachment(string FileName, string MimeType, byte[] Content)
{
    private const string PdfMimeType = "application/pdf";
    private const string PdfExtension = ".pdf";

    public bool IsPdf =>
        MimeType.Equals(PdfMimeType, StringComparison.OrdinalIgnoreCase)
        || FileName.EndsWith(PdfExtension, StringComparison.OrdinalIgnoreCase);
}
