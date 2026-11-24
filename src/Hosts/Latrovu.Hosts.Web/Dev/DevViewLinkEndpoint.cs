using Latrovu.Listings.Viewing;

namespace Latrovu.Hosts.Web.Dev;

public static class DevViewLinkEndpoint
{
    private const string Route = "/dev/view-links/{listingId:guid}";

    public static void MapDevViewLinks(this WebApplication app)
    {
        app.MapPost(Route, CreateAsync);
    }

    private static async Task<IResult> CreateAsync(Guid listingId, HttpRequest request, IListingViewService views, CancellationToken ct)
    {
        string? token = await views.CreateLinkAsync(listingId, searchId: null, ct);
        return token is null
            ? Results.NotFound("Listing missing or not approved")
            : Results.Ok(new { url = $"{request.Scheme}://{request.Host}/i/{token}" });
    }
}
