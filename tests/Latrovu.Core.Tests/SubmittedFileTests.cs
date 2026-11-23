using Latrovu.Core.Submissions;
using Xunit;

namespace Latrovu.Core.Tests;

public sealed class SubmittedFileTests
{
    [Theory]
    [InlineData("expose.pdf", "application/octet-stream", true)]
    [InlineData("expose", "application/pdf", true)]
    [InlineData("EXPOSE.PDF", "application/octet-stream", true)]
    [InlineData("foto.jpg", "image/jpeg", false)]
    public void IsPdf_DetectsByMimeTypeOrExtension(string fileName, string mimeType, bool expected)
    {
        SubmittedFile file = new(fileName, mimeType, []);

        Assert.Equal(expected, file.IsPdf);
    }

    [Theory]
    [InlineData("image/jpeg", true)]
    [InlineData("IMAGE/PNG", true)]
    [InlineData("image/gif", false)]
    [InlineData("application/pdf", false)]
    public void IsImage_OnlyForJpegAndPng(string mimeType, bool expected)
    {
        SubmittedFile file = new("file", mimeType, []);

        Assert.Equal(expected, file.IsImage);
    }
}
