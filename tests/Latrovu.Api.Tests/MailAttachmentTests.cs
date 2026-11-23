using Latrovu.Api.Images;
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

    [Theory]
    [InlineData("image/jpeg", true)]
    [InlineData("IMAGE/PNG", true)]
    [InlineData("image/gif", false)]
    [InlineData("application/pdf", false)]
    public void IsImage_OnlyForJpegAndPng(string mimeType, bool expected)
    {
        MailAttachment attachment = new("file", mimeType, []);

        Assert.Equal(expected, attachment.IsImage);
    }

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    public void ToImageFile_KeepsContentAndSetsExtension(string mimeType, string expectedExtension)
    {
        MailAttachment attachment = new("photo", mimeType, [1, 2, 3]);

        ImageFile image = attachment.ToImageFile();

        Assert.Equal(expectedExtension, image.Extension);
        Assert.Equal(new byte[] { 1, 2, 3 }, image.Content);
    }

    [Fact]
    public void ToImageFile_ForPdf_Throws()
    {
        MailAttachment attachment = new("expose.pdf", "application/pdf", []);

        Assert.Throws<InvalidOperationException>(() => attachment.ToImageFile());
    }
}
