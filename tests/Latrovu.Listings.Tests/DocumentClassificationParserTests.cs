using Latrovu.Listings;
using Xunit;

namespace Latrovu.Listings.Tests;

public sealed class DocumentClassificationParserTests
{
    [Theory]
    [InlineData("single_listing", DocumentType.SingleListing)]
    [InlineData("multi_unit_listing", DocumentType.MultiUnitListing)]
    [InlineData("application_form", DocumentType.ApplicationForm)]
    [InlineData("contract", DocumentType.Contract)]
    [InlineData("OTHER", DocumentType.Other)]
    public void Parse_KnownType_ReturnsMatchingType(string typeName, DocumentType expected)
    {
        string answer = $"{{\"documentType\":\"{typeName}\",\"reason\":\"test\"}}";

        DocumentClassification? actual = DocumentClassificationParser.Parse(answer);

        Assert.NotNull(actual);
        Assert.Equal(expected, actual.Type);
        Assert.Equal("test", actual.Reason);
    }

    [Fact]
    public void Parse_FencedAnswer_IsRead()
    {
        const string answer = "```json\n{\"documentType\":\"application_form\"}\n```";

        DocumentClassification? actual = DocumentClassificationParser.Parse(answer);

        Assert.Equal(DocumentType.ApplicationForm, actual?.Type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("kein JSON")]
    [InlineData("{\"documentType\":\"brochure\"}")]
    [InlineData("{\"reason\":\"no type\"}")]
    public void Parse_UnreadableAnswer_ReturnsNull(string? answer)
    {
        DocumentClassification? actual = DocumentClassificationParser.Parse(answer);

        Assert.Null(actual);
    }

    [Theory]
    [InlineData(DocumentType.SingleListing, true)]
    [InlineData(DocumentType.MultiUnitListing, true)]
    [InlineData(DocumentType.ApplicationForm, false)]
    [InlineData(DocumentType.Contract, false)]
    [InlineData(DocumentType.Other, false)]
    public void IsListing_OnlyForListingTypes(DocumentType type, bool expected)
    {
        DocumentClassification classification = new(type, null);

        Assert.Equal(expected, classification.IsListing);
    }
}
