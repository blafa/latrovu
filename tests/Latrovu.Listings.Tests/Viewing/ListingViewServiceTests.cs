using Microsoft.Extensions.Logging.Abstractions;

namespace Latrovu.Listings.Tests.Viewing;

public sealed class ListingViewServiceTests
{
    private static readonly Guid ListingId = Guid.NewGuid();

    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly FakeListingReviewStore listings = new();
    private readonly FakeListingViewLinkStore links = new();
    private readonly ListingViewService service;

    public ListingViewServiceTests()
    {
        service = new ListingViewService(links, listings, time, NullLogger<ListingViewService>.Instance);
    }

    [Fact]
    public async Task CreateLink_ForDraft_ReturnsNull()
    {
        string? token = await service.CreateLinkAsync(ListingId, searchId: null, CancellationToken.None);

        Assert.Null(token);
    }

    [Fact]
    public async Task Open_ApprovedListing_ReturnsItAndCountsView()
    {
        await listings.ApproveAsync(ListingId, CancellationToken.None);
        string token = (await service.CreateLinkAsync(ListingId, searchId: null, CancellationToken.None))!;

        ListingReview? listing = await service.OpenAsync(token, CancellationToken.None);

        Assert.NotNull(listing);
        Assert.Equal(1, links.Views[token]);
    }

    [Fact]
    public async Task Find_DoesNotCountView()
    {
        await listings.ApproveAsync(ListingId, CancellationToken.None);
        string token = (await service.CreateLinkAsync(ListingId, searchId: null, CancellationToken.None))!;

        await service.FindAsync(token, CancellationToken.None);

        Assert.False(links.Views.ContainsKey(token));
    }

    [Fact]
    public async Task Open_AfterWithdrawal_ReturnsNull()
    {
        await listings.ApproveAsync(ListingId, CancellationToken.None);
        string token = (await service.CreateLinkAsync(ListingId, searchId: null, CancellationToken.None))!;
        await listings.WithdrawAsync(ListingId, CancellationToken.None);

        Assert.Null(await service.OpenAsync(token, CancellationToken.None));
    }

    [Fact]
    public async Task Open_ExpiredLink_ReturnsNull()
    {
        await listings.ApproveAsync(ListingId, CancellationToken.None);
        string token = (await service.CreateLinkAsync(ListingId, searchId: null, CancellationToken.None))!;
        time.Advance(ListingViewService.LinkValidity + TimeSpan.FromMinutes(1));

        Assert.Null(await service.OpenAsync(token, CancellationToken.None));
    }
}
