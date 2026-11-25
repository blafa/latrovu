using Latrovu.Listings.Renewal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Latrovu.Listings.Infrastructure.Renewal;

public static class ListingRenewalEndpoints
{
    private const string HtmlContentType = "text/html; charset=utf-8";
    private const string RobotsHeader = "X-Robots-Tag";
    private const string NoIndex = "noindex, nofollow";

    public static IEndpointRouteBuilder MapListingRenewal(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder renew = app.MapGroup("/renew/{token}");
        renew.MapGet(string.Empty, ShowAsync);
        renew.MapPost(string.Empty, RenewAsync);
        return app;
    }

    private static async Task<IResult> ShowAsync(string token, HttpResponse response, ListingRenewalService renewals, CancellationToken ct)
    {
        response.Headers[RobotsHeader] = NoIndex;
        return await renewals.IsValidAsync(token, ct)
            ? Html(RenewalPage.Ask())
            : Html(RenewalPage.Invalid(), StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> RenewAsync(string token, HttpResponse response, ListingRenewalService renewals, CancellationToken ct)
    {
        response.Headers[RobotsHeader] = NoIndex;
        return await renewals.RenewAsync(token, ct)
            ? Html(RenewalPage.Done())
            : Html(RenewalPage.Invalid(), StatusCodes.Status404NotFound);
    }

    private static IResult Html(string content, int statusCode = StatusCodes.Status200OK) =>
        Results.Content(content, HtmlContentType, statusCode: statusCode);
}
