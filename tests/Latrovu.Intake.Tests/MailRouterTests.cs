using Latrovu.Intake;
using Xunit;

namespace Latrovu.Intake.Tests;

public sealed class MailRouterTests
{
    private readonly MailRouter router = new();

    [Theory]
    [InlineData("inserat@latrovu.com", MailKind.Listing)]
    [InlineData("INSERAT@latrovu.com", MailKind.Listing)]
    [InlineData("suche@latrovu.com", MailKind.Search)]
    [InlineData("fbl@latrovu.com", MailKind.Unknown)]
    public void Classify_SingleRecipient_ReturnsExpectedKind(string recipient, MailKind expected)
    {
        MailKind actual = router.Classify([recipient]);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Classify_ListingAmongOtherRecipients_ReturnsListing()
    {
        MailKind actual = router.Classify(["someone@example.com", "inserat@latrovu.com"]);

        Assert.Equal(MailKind.Listing, actual);
    }

    [Fact]
    public void Classify_NoRecipients_ReturnsUnknown()
    {
        MailKind actual = router.Classify([]);

        Assert.Equal(MailKind.Unknown, actual);
    }

    [Fact]
    public void Classify_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => router.Classify(null!));
    }
}
