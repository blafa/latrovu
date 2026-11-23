using Latrovu.BuildingBlocks.Images;
using Latrovu.Core.Submissions;
using Xunit;

namespace Latrovu.Listings.Tests.Images;

public sealed class SubmittedFileImagesTests
{
    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    public void ToImageFile_KeepsContentAndSetsExtension(string mimeType, string expectedExtension)
    {
        SubmittedFile file = new("photo", mimeType, [1, 2, 3]);

        ImageFile image = SubmittedFileImages.ToImageFile(file);

        Assert.Equal(expectedExtension, image.Extension);
        Assert.Equal(new byte[] { 1, 2, 3 }, image.Content);
    }

    [Fact]
    public void ToImageFile_ForPdf_Throws()
    {
        SubmittedFile file = new("expose.pdf", "application/pdf", []);

        Assert.Throws<InvalidOperationException>(() => SubmittedFileImages.ToImageFile(file));
    }
}
