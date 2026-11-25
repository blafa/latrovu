using Latrovu.Intake;
using Xunit;

namespace Latrovu.Intake.Tests;

public sealed class QuotedReplyStripperTests
{
    [Theory]
    [InlineData("stop\n\nAm 24.11.2026 um 14:32 schrieb Latrovu <fbl@latrovu.com>:\n> Guten Tag")]
    [InlineData("stop\n\nOn Tue, 24 Nov 2026 at 14:32, Latrovu wrote:\n> Hello")]
    [InlineData("stop\n\nLe mar. 24 nov. 2026 \u00e0 14:32, Latrovu a \u00e9crit :\n> Bonjour")]
    [InlineData("stop\n\n-----Original Message-----\nFrom: Latrovu")]
    [InlineData("stop\n\nVon: Latrovu <fbl@latrovu.com>\nGesendet: Dienstag")]
    [InlineData("stop\n> quoted line")]
    [InlineData("stop\n\nAm Di., 24. November 2026 um 20:25 hat Latrovu <fbl@latrovu.com> geschrieben:\n> Guten Tag")]
    [InlineData("stop\n\nAm Mi., 25. November 2026 um 06:56 hat Fabio Blasi\n<fbl@latrovu.com> geschrieben:\n> Guten Tag")]
    public void Strip_RemovesQuotedPart(string body)
    {
        Assert.Equal("stop", QuotedReplyStripper.Strip(body));
    }

    [Fact]
    public void Strip_KeepsMessageWithoutQuote()
    {
        Assert.Equal("3 Zimmer in Bern\nbis 2500", QuotedReplyStripper.Strip("3 Zimmer in Bern\r\nbis 2500\r\n"));
    }

    [Fact]
    public void Strip_KeepsOwnSentenceStartingWithAm()
    {
        Assert.Equal("Am 1. Dezember ziehe ich um.\nBitte melden Sie sich.", QuotedReplyStripper.Strip("Am 1. Dezember ziehe ich um.\nBitte melden Sie sich."));
    }
}
