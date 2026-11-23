using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Latrovu.Listings.Tests.Review;

public sealed class ListingReviewServiceTests
{
    private const string Token = "valid-token";

    private readonly FakeListingReviewStore reviews = new();
    private readonly ListingReviewService service;

    public ListingReviewServiceTests()
    {
        FakeListingAccessTokenStore tokens = new(Token, Guid.NewGuid());
        service = new ListingReviewService(tokens, reviews, NullLogger<ListingReviewService>.Instance);
    }

    [Fact]
    public async Task UpdateField_SavesValueAndMarksConfirmed()
    {
        FieldUpdateResult result = await service.UpdateFieldAsync(Token, "size.rooms", "3.5", CancellationToken.None);

        Assert.Equal(FieldUpdateResult.Saved, result);
        Assert.Equal(3.5m, JsonNode.Parse(reviews.DataJson)!["size"]!["rooms"]!.GetValue<decimal>());
        Assert.Equal(ListingReviewService.ConfirmedScore, JsonNode.Parse(reviews.ScoresJson)!["size.rooms"]!.GetValue<string>());
    }

    [Fact]
    public async Task UpdateField_InvalidValue_DoesNotSave()
    {
        FieldUpdateResult result = await service.UpdateFieldAsync(Token, "size.rooms", "drei", CancellationToken.None);

        Assert.Equal(FieldUpdateResult.InvalidValue, result);
        Assert.Equal("{}", reviews.DataJson);
    }

    [Fact]
    public async Task UpdateField_UnknownField_IsRejected()
    {
        FieldUpdateResult result = await service.UpdateFieldAsync(Token, "size.castles", "1", CancellationToken.None);

        Assert.Equal(FieldUpdateResult.UnknownField, result);
    }

    [Fact]
    public async Task UpdateField_UnknownToken_IsNotFound()
    {
        FieldUpdateResult result = await service.UpdateFieldAsync("wrong", "size.rooms", "3", CancellationToken.None);

        Assert.Equal(FieldUpdateResult.NotFound, result);
    }

    [Fact]
    public async Task ConfirmField_MarksConfirmedWithoutChangingData()
    {
        await service.UpdateFieldAsync(Token, "size.rooms", "2.5", CancellationToken.None);
        string dataBefore = reviews.DataJson;

        FieldUpdateResult result = await service.ConfirmFieldAsync(Token, "location.city", CancellationToken.None);

        Assert.Equal(FieldUpdateResult.Saved, result);
        Assert.Equal(dataBefore, reviews.DataJson);
        Assert.Equal(ListingReviewService.ConfirmedScore, JsonNode.Parse(reviews.ScoresJson)!["location.city"]!.GetValue<string>());
    }

    [Fact]
    public async Task AddCustomDetail_AppendsToOtherDetails()
    {
        await service.AddCustomDetailAsync(Token, "Keller", "8 m²", CancellationToken.None);
        await service.AddCustomDetailAsync(Token, " Estrich ", " ja ", CancellationToken.None);

        JsonArray details = JsonNode.Parse(reviews.DataJson)![ListingReviewService.OtherDetailsPath]!.AsArray();
        Assert.Equal(2, details.Count);
        Assert.Equal("Estrich", details[1]!["label"]!.GetValue<string>());
        Assert.Equal("ja", details[1]!["value"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("", "ja")]
    [InlineData("Keller", " ")]
    public async Task AddCustomDetail_BlankLabelOrValue_IsInvalid(string label, string value)
    {
        FieldUpdateResult result = await service.AddCustomDetailAsync(Token, label, value, CancellationToken.None);

        Assert.Equal(FieldUpdateResult.InvalidValue, result);
    }

    [Fact]
    public async Task PublishContact_IsOffByDefaultAndCanBeSwitched()
    {
        Assert.False(reviews.PublishContact);

        bool saved = await service.SetPublishContactAsync(Token, true, CancellationToken.None);

        Assert.True(saved);
        Assert.True(reviews.PublishContact);
    }

    [Fact]
    public async Task PublishContact_UnknownToken_IsNotSaved()
    {
        bool saved = await service.SetPublishContactAsync("wrong", true, CancellationToken.None);

        Assert.False(saved);
        Assert.False(reviews.PublishContact);
    }
}
