namespace Latrovu.Intake.Tests;

public sealed class AutomaticMailTests
{
    [Theory]
    [InlineData("auto-replied", null, false, "agent@example.com")]
    [InlineData("auto-generated", null, false, "agent@example.com")]
    [InlineData(null, "bulk", false, "agent@example.com")]
    [InlineData(null, "junk", false, "agent@example.com")]
    [InlineData(null, null, true, "agent@example.com")]
    [InlineData(null, null, false, "MAILER-DAEMON@mail.example.com")]
    [InlineData(null, null, false, "noreply@portal.example.com")]
    public void IsAutomatic_AutoRepliesBouncesAndBulk_AreDetected(string? autoSubmitted, string? precedence, bool autoReplyHeader, string sender)
    {
        Assert.True(AutomaticMail.IsAutomatic(autoSubmitted, precedence, autoReplyHeader, sender));
    }

    [Theory]
    [InlineData(null, null, false, "agent@example.com")]
    [InlineData("no", null, false, "agent@example.com")]
    [InlineData(null, "normal", false, "selin.kaya@example.com")]
    public void IsAutomatic_PersonalMail_IsNotAutomatic(string? autoSubmitted, string? precedence, bool autoReplyHeader, string sender)
    {
        Assert.False(AutomaticMail.IsAutomatic(autoSubmitted, precedence, autoReplyHeader, sender));
    }
}
