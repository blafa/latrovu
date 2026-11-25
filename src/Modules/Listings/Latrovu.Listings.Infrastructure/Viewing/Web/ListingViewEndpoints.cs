using Latrovu.BuildingBlocks.Storage;
using Latrovu.Listings.Viewing.Access;
using Latrovu.Listings.Viewing.Chat;
using Latrovu.Listings.Viewing.Viewings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Latrovu.Listings.Infrastructure.Viewing.Web;

public static class ListingViewEndpoints
{
    public const string RateLimitPolicy = "listing-views";
    public const string ImageRateLimitPolicy = "listing-images";
    private const string ImageCacheControl = "private, max-age=86400";
    private const string HtmlContentType = "text/html; charset=utf-8";
    private const string RobotsHeader = "X-Robots-Tag";
    private const string NoIndex = "noindex, nofollow";
    private const string QuestionFormKey = "question";
    private const string AnswerIdFormKey = "answerId";
    private const string EmailFormKey = "email";
    private const string FallbackLanguage = "de";

    public static IEndpointRouteBuilder MapListingViews(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder views = app.MapGroup("/i/{token}").RequireRateLimiting(RateLimitPolicy);
        views.MapGet(string.Empty, ShowAsync);
        views.MapPost("/share", ShareAsync);
        views.MapPost("/access", RequestAccessAsync);
        views.MapPost("/chat", AskAsync);
        views.MapPost("/forward", ForwardAsync);
        views.MapPost("/viewings/{slotId:long}/book", BookViewingAsync);
        views.MapPost("/viewings/cancel", CancelViewingAsync);
        app.MapGroup("/i/{token}/images").RequireRateLimiting(ImageRateLimitPolicy).MapGet("/{position:int}", ImageAsync);
        return app;
    }

    private static async Task<IResult> ShowAsync(
        string token,
        HttpResponse response,
        IListingViewService views,
        ListingChatService chat,
        SeekerViewingService viewings,
        CancellationToken ct)
    {
        response.Headers[RobotsHeader] = NoIndex;
        ListingView? view = await views.OpenAsync(token, ct);
        if (view is null)
        {
            return Results.Content(ListingViewPage.RenderUnavailable(), HtmlContentType, statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Content(
            ListingViewPage.Render(token, view, await chat.GetAsync(token, ct), await viewings.GetAsync(token, ct)),
            HtmlContentType);
    }

    private static async Task<IResult> ShareAsync(string token, HttpResponse response, IListingViewService views, CancellationToken ct)
    {
        response.Headers[RobotsHeader] = NoIndex;
        string? shared = await views.ShareAsync(token, ct);
        return shared is null ? Results.NotFound() : Results.Ok(new { path = $"/i/{shared}" });
    }

    private static async Task<IResult> RequestAccessAsync(string token, HttpRequest request, ListingAccessService access, CancellationToken ct)
    {
        request.HttpContext.Response.Headers[RobotsHeader] = NoIndex;
        IFormCollection form = await request.ReadFormAsync(ct);
        AccessOutcome outcome = await access.RequestAsync(token, form[EmailFormKey].ToString(), PreferredLanguage(request), ct);
        return outcome == AccessOutcome.NotFound
            ? Results.NotFound()
            : Results.Content(UnlockDialog.Result(token, outcome), HtmlContentType);
    }

    private static string PreferredLanguage(HttpRequest request)
    {
        string? preferred = request.GetTypedHeaders().AcceptLanguage
            .OrderByDescending(language => language.Quality ?? 1)
            .Select(language => language.Value.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value) && value != "*");
        return preferred?.Split('-')[0] ?? FallbackLanguage;
    }

    private static async Task<IResult> BookViewingAsync(
        string token,
        long slotId,
        HttpResponse response,
        SeekerViewingService viewings,
        ListingChatService chat,
        CancellationToken ct)
    {
        response.Headers[RobotsHeader] = NoIndex;
        ViewingResult result = await viewings.BookAsync(token, slotId, ct);
        if (!result.Found)
        {
            return Results.NotFound();
        }

        string chatUpdate = ListingChatPanel.Render(token, await chat.GetAsync(token, ct))
            .Replace("<section id=\"chat\"", "<section hx-swap-oob=\"true\" id=\"chat\"", StringComparison.Ordinal);
        return Results.Content(SeekerViewingPanel.Render(token, result) + chatUpdate, HtmlContentType);
    }

    private static async Task<IResult> CancelViewingAsync(string token, HttpResponse response, SeekerViewingService viewings, CancellationToken ct)
    {
        response.Headers[RobotsHeader] = NoIndex;
        ViewingResult result = await viewings.CancelAsync(token, ct);
        return result.Found ? Results.Content(SeekerViewingPanel.Render(token, result), HtmlContentType) : Results.NotFound();
    }

    private static async Task<IResult> AskAsync(
        string token,
        HttpRequest request,
        ListingChatService chat,
        SeekerViewingService viewings,
        CancellationToken ct)
    {
        request.HttpContext.Response.Headers[RobotsHeader] = NoIndex;
        IFormCollection form = await request.ReadFormAsync(ct);
        ChatResult result = await chat.AskAsync(token, form[QuestionFormKey].ToString(), ct);
        if (result.Outcome == ChatOutcome.NotFound)
        {
            return Results.NotFound();
        }

        string viewingsUpdate = SeekerViewingPanel.Render(token, await viewings.GetAsync(token, ct))
            .Replace("<section id=\"viewings\"", "<section hx-swap-oob=\"true\" id=\"viewings\"", StringComparison.Ordinal);
        return Results.Content(ListingChatPanel.Render(token, result) + viewingsUpdate, HtmlContentType);
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
        if (stored is null)
        {
            return Results.NotFound();
        }

        response.Headers.CacheControl = ImageCacheControl;
        return Results.File(stored.Content, stored.ContentType);
    }
}
