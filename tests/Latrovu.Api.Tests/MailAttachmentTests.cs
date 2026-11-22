using Latrovu.Api.Mail;
using Xunit;

namespace Latrovu.Api.Tests;

public sealed class MailAttachmentTests
{
    [Theory]
    [InlineData("expose.pdf", "application/octet-stream", true)]
    [InlineData("expose", "application/pdf", true)]
    [InlineData("EXPOSE.PDF", "application/octet-stream", true)]
    [InlineData("foto.jpg", "image/jpeg", false)]
    public void IsPdf_DetectsByMimeTypeOrExtension(string fileName, string mimeType, bool expected)
    {
        MailAttachment attachment = new(fileName, mimeType, []);

        Assert.Equal(expected, attachment.IsPdf);
    }
}
