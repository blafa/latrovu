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
}
