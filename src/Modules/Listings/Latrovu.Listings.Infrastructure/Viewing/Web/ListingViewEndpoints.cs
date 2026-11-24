using Latrovu.BuildingBlocks.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Latrovu.Listings.Infrastructure.Viewing.Web;

public static class ListingViewEndpoints
{
    public const string RateLimitPolicy = "listing-views";
    private const string HtmlContentType = "text/html; charset=utf-8";
    private const string RobotsHeader = "X-Robots-Tag";
    private const string NoIndex = "noindex, nofollow";

    public static IEndpointRouteBuilder MapListingViews(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder views = app.MapGroup("/i/{token}").RequireRateLimiting(RateLimitPolicy);
        views.MapGet(string.Empty, ShowAsync);
        views.MapGet("/images/{position:int}", ImageAsync);
        return app;
    }

    private static async Task<IResult> ShowAsync(
        string token,
        HttpResponse response,
        IListingViewService views,
        IOptions<ViewingOptions> options,
        CancellationToken ct)
    {
        response.Headers[RobotsHeader] = NoIndex;
        ListingReview? listing = await views.OpenAsync(token, ct);
        return listing is null
            ? Results.Content(ListingViewPage.RenderUnavailable(), HtmlContentType, statusCode: StatusCodes.Status404NotFound)
            : Results.Content(ListingViewPage.Render(token, listing, options.Value.InquiryAddress), HtmlContentType);
    }

    private static async Task<IResult> ImageAsync(
        string token,
        int position,
        HttpResponse response,
        IListingViewService views,
        IObjectStorage storage,
        CancellationToken ct)
    {
        response.Headers[RobotsHeader] = NoIndex;
        ListingReview? listing = await views.FindAsync(token, ct);
        ListingImageRef? image = listing?.Images.FirstOrDefault(candidate => candidate.Position == position);
        StoredObject? stored = image is null ? null : await storage.GetAsync(image.StorageKey, ct);
        return stored is null ? Results.NotFound() : Results.File(stored.Content, stored.ContentType);
    }
}
