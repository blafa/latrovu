using Latrovu.BuildingBlocks.Images;
using Latrovu.Core.Messaging;
using Latrovu.Listings;
using Xunit;

namespace Latrovu.Listings.Tests;

public sealed class ReceivedFileImagesTests
{
    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    public void ToImageFile_KeepsContentAndSetsExtension(string mimeType, string expectedExtension)
    {
        ReceivedFile file = new("photo", mimeType, [1, 2, 3]);

        ImageFile image = ReceivedFileImages.ToImageFile(file);

        Assert.Equal(expectedExtension, image.Extension);
        Assert.Equal(new byte[] { 1, 2, 3 }, image.Content);
    }

    [Fact]
    public void ToImageFile_ForPdf_Throws()
    {
        ReceivedFile file = new("expose.pdf", "application/pdf", []);

        Assert.Throws<InvalidOperationException>(() => ReceivedFileImages.ToImageFile(file));
    }
}
