using Latrovu.Listings.Infrastructure.Viewing.Chat;

namespace Latrovu.Listings.Tests.Viewing;

public sealed class ContactRedactorTests
{
    private const string ListingData = """
        { "title": "Haus in Thun",
          "contact": { "company": "reoplan Immobilien AG", "name": "Petra Muster", "phone": "033 222 11 00", "email": "info@reoplan.ch" } }
        """;

    private readonly ContactRedactor redactor = ContactRedactor.FromListingData(ListingData);

    [Fact]
    public void Redact_CompanyAndPersonNames_AreReplaced()
    {
        string result = redactor.Redact("Besichtigungen mit Petra Muster von der reoplan Immobilien AG.", "der Anbieter");

        Assert.DoesNotContain("reoplan", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Petra", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_ShortCompanyName_IsReplaced()
    {
        string result = redactor.Redact("Wenden Sie sich an Reoplan.", "der Anbieter");

        Assert.Equal("Wenden Sie sich an der Anbieter.", result);
    }

    [Fact]
    public void Redact_EmailAndPhone_AreRemoved()
    {
        string result = redactor.Redact("Schreiben Sie an kontakt@example.ch oder rufen Sie +41 79 123 45 67 an.", "der Anbieter");

        Assert.DoesNotContain("@", result, StringComparison.Ordinal);
        Assert.DoesNotContain("123 45 67", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_Prices_StayUntouched()
    {
        string result = redactor.Redact("Der Kaufpreis beträgt CHF 860'000 bzw. 1 250 000 mit Garage.", "der Anbieter");

        Assert.Contains("860'000", result, StringComparison.Ordinal);
        Assert.Contains("1 250 000", result, StringComparison.Ordinal);
    }
}
