namespace Latrovu.Listings.Tests.Ingestion;

public sealed class IntakeReportTests
{
    [Fact]
    public void Report_WithoutFiles_HasNoPdfAndNoListings()
    {
        IntakeReport report = new("agent@example.com", "Exposé", []);

        Assert.True(report.HasNoPdf);
        Assert.False(report.HasListings);
    }

    [Fact]
    public void Report_OnlyForms_HasNoListings()
    {
        IntakeReport report = new("agent@example.com", null,
            [new IntakeFileOutcome("anmeldung.pdf", IntakeFileStatus.NotAListing, DocumentType: DocumentType.ApplicationForm)]);

        Assert.False(report.HasNoPdf);
        Assert.False(report.HasListings);
    }

    [Theory]
    [InlineData(IntakeFileStatus.Created)]
    [InlineData(IntakeFileStatus.AlreadyKnown)]
    public void Report_WithCreatedOrKnownListing_HasListings(IntakeFileStatus status)
    {
        IntakeReport report = new("agent@example.com", null,
            [new IntakeFileOutcome("anmeldung.pdf", IntakeFileStatus.NotAListing), new IntakeFileOutcome("expose.pdf", status, "token")]);

        Assert.True(report.HasListings);
    }
}
