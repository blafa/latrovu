using Latrovu.Listings.Viewing.Access;
using Microsoft.Extensions.Logging.Abstractions;

namespace Latrovu.Listings.Tests.Viewing;

public sealed class ListingAccessServiceTests
{
    private const string Address = "friend@example.com";
    private const string Language = "fr";

    private static readonly Guid ListingId = Guid.NewGuid();

    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly FakeListingReviewStore listings = new();
    private readonly FakeListingViewLinkStore links = new();
    private readonly FakeListingAccessRequestStore requests = new();
    private readonly ListingViewService views;
    private readonly ListingAccessService service;

    public ListingAccessServiceTests()
    {
        views = new ListingViewService(links, listings, time, NullLogger<ListingViewService>.Instance);
        service = new ListingAccessService(views, requests, time, NullLogger<ListingAccessService>.Instance);
    }

    [Fact]
    public async Task Request_SharedLink_RecordsRequestInLanguage()
    {
        string shared = await SharedLinkAsync();

        AccessOutcome outcome = await service.RequestAsync(shared, $"  {Address} ", Language, CancellationToken.None);

        Assert.Equal(AccessOutcome.Sent, outcome);
        Assert.Equal(Address, Assert.Single(requests.Requests).Address);
        Assert.Equal(Language, requests.Requests[0].Language);
    }

    [Fact]
    public async Task Request_PersonalLink_IsNotFound()
    {
        await listings.ApproveAsync(ListingId, CancellationToken.None);
        string personal = (await views.CreateLinkAsync(ListingId, searchId: null, CancellationToken.None))!;

        AccessOutcome outcome = await service.RequestAsync(personal, Address, Language, CancellationToken.None);

        Assert.Equal(AccessOutcome.NotFound, outcome);
        Assert.Empty(requests.Requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-at-sign")]
    [InlineData("two words@example.com")]
    public async Task Request_InvalidAddress_IsRejected(string address)
    {
        AccessOutcome outcome = await service.RequestAsync(await SharedLinkAsync(), address, Language, CancellationToken.None);

        Assert.Equal(AccessOutcome.InvalidAddress, outcome);
    }

    [Fact]
    public async Task Request_AfterLinkLimit_IsRefused()
    {
        string shared = await SharedLinkAsync();
        for (int person = 0; person < ListingAccessService.MaxRequestsPerLink; person++)
        {
            await service.RequestAsync(shared, $"person{person}@example.com", Language, CancellationToken.None);
        }

        AccessOutcome outcome = await service.RequestAsync(shared, Address, Language, CancellationToken.None);

        Assert.Equal(AccessOutcome.LimitReached, outcome);
    }

    [Fact]
    public async Task Request_AfterAddressLimit_IsRefusedUntilWindowPassed()
    {
        string shared = await SharedLinkAsync();
        for (int attempt = 0; attempt < ListingAccessService.MaxRequestsPerAddress; attempt++)
        {
            await service.RequestAsync(shared, Address, Language, CancellationToken.None);
        }

        AccessOutcome refused = await service.RequestAsync(shared, Address.ToUpperInvariant(), Language, CancellationToken.None);
        time.Advance(ListingAccessService.AddressWindow + TimeSpan.FromMinutes(1));
        AccessOutcome later = await service.RequestAsync(shared, Address, Language, CancellationToken.None);

        Assert.Equal(AccessOutcome.LimitReached, refused);
        Assert.Equal(AccessOutcome.Sent, later);
    }

    private async Task<string> SharedLinkAsync()
    {
        await listings.ApproveAsync(ListingId, CancellationToken.None);
        string personal = (await views.CreateLinkAsync(ListingId, searchId: null, CancellationToken.None))!;
        return (await views.ShareAsync(personal, CancellationToken.None))!;
    }
}
