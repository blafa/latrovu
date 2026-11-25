using Latrovu.Intake;
using Xunit;

namespace Latrovu.Intake.Tests;

public sealed class AgentReplyReferenceTests
{
    [Theory]
    [InlineData("Re: Anfrage zu Ihrem Inserat «Haus in Thun» [Q-1a2b3c4d5e]", "Q-1a2b3c4d5e")]
    [InlineData("AW: Anfrage [q-1A2B3C4D5E]", "Q-1a2b3c4d5e")]
    public void Find_SubjectWithReference_ReturnsNormalizedReference(string subject, string expected)
    {
        Assert.Equal(expected, AgentReplyReference.Find(subject));
    }

    [Theory]
    [InlineData("Neues Inserat Thun")]
    [InlineData("Q-1a2b3c4d5e ohne Klammern")]
    [InlineData(null)]
    public void Find_SubjectWithoutReference_ReturnsNull(string? subject)
    {
        Assert.Null(AgentReplyReference.Find(subject));
    }
}
