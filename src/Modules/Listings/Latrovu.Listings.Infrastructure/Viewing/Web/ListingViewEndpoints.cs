using Latrovu.BuildingBlocks.Storage;
using Latrovu.Listings.Viewing.Chat;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Latrovu.Listings.Infrastructure.Viewing.Web;

public static class ListingViewEndpoints
{
    public const string RateLimitPolicy = "listing-views";
    private const string HtmlContentType = "text/html; charset=utf-8";
    private const string RobotsHeader = "X-Robots-Tag";
    private const string NoIndex = "noindex, nofollow";
    private const string QuestionFormKey = "question";
    private const string AnswerIdFormKey = "answerId";

    public static IEndpointRouteBuilder MapListingViews(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder views = app.MapGroup("/i/{token}").RequireRateLimiting(RateLimitPolicy);
        views.MapGet(string.Empty, ShowAsync);
        views.MapPost("/chat", AskAsync);
        views.MapPost("/forward", ForwardAsync);
        views.MapGet("/images/{position:int}", ImageAsync);
        return app;
    }

    private static async Task<IResult> ShowAsync(
        string token,
        HttpResponse response,
        IListingViewService views,
        ListingChatService chat,
        CancellationToken ct)
    {
        response.Headers[RobotsHeader] = NoIndex;
        ListingReview? listing = await views.OpenAsync(token, ct);
        if (listing is null)
        {
            return Results.Content(ListingViewPage.RenderUnavailable(), HtmlContentType, statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Content(ListingViewPage.Render(token, listing, await chat.GetAsync(token, ct)), HtmlContentType);
    }

    private static async Task<IResult> AskAsync(string token, HttpRequest request, ListingChatService chat, CancellationToken ct)
    {
        request.HttpContext.Response.Headers[RobotsHeader] = NoIndex;
        IFormCollection form = await request.ReadFormAsync(ct);
        ChatResult result = await chat.AskAsync(token, form[QuestionFormKey].ToString(), ct);
        return result.Outcome == ChatOutcome.NotFound
            ? Results.NotFound()
            : Results.Content(ListingChatPanel.Render(token, result), HtmlContentType);
    }

    private static async Task<IResult> ForwardAsync(string token, HttpRequest request, ListingChatService chat, CancellationToken ct)
    {
        request.HttpContext.Response.Headers[RobotsHeader] = NoIndex;
        IFormCollection form = await request.ReadFormAsync(ct);
        if (!long.TryParse(form[AnswerIdFormKey], System.Globalization.CultureInfo.InvariantCulture, out long answerId))
        {
            return Results.BadRequest();
        }

        ChatResult result = await chat.ForwardAsync(token, answerId, ct);
        return result.Outcome == ChatOutcome.NotFound
            ? Results.NotFound()
            : Results.Content(ListingChatPanel.Render(token, result with { Outcome = ChatOutcome.Answered }), HtmlContentType);
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
