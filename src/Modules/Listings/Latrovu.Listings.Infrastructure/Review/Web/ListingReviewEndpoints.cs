using System.Text.Json.Nodes;
using Latrovu.BuildingBlocks.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Latrovu.Listings.Infrastructure.Review.Web;

public static class ListingReviewEndpoints
{
    public const string SessionCookieName = "latrovu_review_session";
    private const string HtmlContentType = "text/html; charset=utf-8";
    private const string PathFormKey = "path";
    private const string ValueFormKey = "value";
    private const string CodeFormKey = "code";
    private const string LabelFormKey = "label";
    private const string HtmxRedirectHeader = "HX-Redirect";
    private const string HtmxRefreshHeader = "HX-Refresh";
    private const string HtmxTrue = "true";
    private const string CheckedFormValue = "true";

    public static IEndpointRouteBuilder MapListingReview(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder review = app.MapGroup("/review/{token}");
        review.MapGet(string.Empty, ShowAsync);
        review.MapPost("/code", RequestCodeAsync);
        review.MapPost("/verify", VerifyCodeAsync);
        review.MapPost("/fields", UpdateFieldAsync);
        review.MapPost("/confirm", ConfirmFieldAsync);
        review.MapPost("/details", AddCustomDetailAsync);
        review.MapPost("/publish-contact", SetPublishContactAsync);
        review.MapPost("/approve", ApproveAsync);
        review.MapGet("/images/{position:int}", ImageAsync);
        return app;
    }

    private static async Task<IResult> ShowAsync(
        string token,
        HttpRequest request,
        IReviewAccessService access,
        IListingReviewService reviews,
        CancellationToken ct)
    {
        if (!await HasSessionAsync(token, request, access, ct))
        {
            return Html(ReviewGatePage.RenderGate(token));
        }

        ListingReview? review = await reviews.GetByTokenAsync(token, ct);
        return review is null ? Results.NotFound() : Html(ListingReviewPage.Render(token, review));
    }

    private static async Task<IResult> RequestCodeAsync(string token, IReviewAccessService access, CancellationToken ct)
    {
        CodeRequestResult result = await access.RequestCodeAsync(token, ct);
        return Html(ReviewGatePage.RenderCodeRequestResult(token, result));
    }

    private static async Task<IResult> VerifyCodeAsync(
        string token,
        HttpContext context,
        IReviewAccessService access,
        CancellationToken ct)
    {
        IFormCollection form = await context.Request.ReadFormAsync(ct);
        CodeVerificationResult result = await access.VerifyCodeAsync(token, form[CodeFormKey].ToString(), ct);
        if (result.Outcome != CodeVerificationOutcome.Verified || result.SessionToken is null)
        {
            return Html(ReviewGatePage.RenderVerificationFailure(result.Outcome));
        }

        context.Response.Cookies.Append(SessionCookieName, result.SessionToken, SessionCookieOptions(token, context.Request));
        context.Response.Headers[HtmxRedirectHeader] = $"/review/{token}";
        return Html(string.Empty);
    }

    private static async Task<IResult> UpdateFieldAsync(
        string token,
        HttpRequest request,
        IReviewAccessService access,
        IListingReviewService reviews,
        CancellationToken ct)
    {
        if (!await HasSessionAsync(token, request, access, ct))
        {
            return Results.Unauthorized();
        }

        IFormCollection form = await request.ReadFormAsync(ct);
        string path = form[PathFormKey].ToString();
        FieldUpdateResult result = await reviews.UpdateFieldAsync(token, path, form[ValueFormKey].ToString(), ct);
        string status = ListingReviewPage.RenderFieldStatus(result);
        if (result != FieldUpdateResult.Saved)
        {
            return Html(status);
        }

        ListingReview? review = await reviews.GetByTokenAsync(token, ct);
        return Html(status + SavedValueFragment(review, path));
    }

    private static string SavedValueFragment(ListingReview? review, string path)
    {
        FieldDefinition? field = ListingFieldCatalog.Find(path);
        if (review is null || field is null || JsonNode.Parse(review.DataJson) is not JsonObject data)
        {
            return string.Empty;
        }

        return ReviewSummary.SavedValueOob(field, JsonPaths.Get(data, path));
    }

    private static async Task<IResult> ConfirmFieldAsync(
        string token,
        HttpContext context,
        IReviewAccessService access,
        IListingReviewService reviews,
        CancellationToken ct)
    {
        if (!await HasSessionAsync(token, context.Request, access, ct))
        {
            return Results.Unauthorized();
        }

        IFormCollection form = await context.Request.ReadFormAsync(ct);
        FieldUpdateResult result = await reviews.ConfirmFieldAsync(token, form[PathFormKey].ToString(), ct);
        return RefreshOnSuccess(context, result);
    }

    private static async Task<IResult> AddCustomDetailAsync(
        string token,
        HttpContext context,
        IReviewAccessService access,
        IListingReviewService reviews,
        CancellationToken ct)
    {
        if (!await HasSessionAsync(token, context.Request, access, ct))
        {
            return Results.Unauthorized();
        }

        IFormCollection form = await context.Request.ReadFormAsync(ct);
        string label = form[LabelFormKey].ToString();
        string value = form[ValueFormKey].ToString();
        FieldUpdateResult result = await TrySetKnownFieldAsync(token, label, value, reviews, ct);
        if (result != FieldUpdateResult.Saved)
        {
            result = await reviews.AddCustomDetailAsync(token, label, value, ct);
        }

        return RefreshOnSuccess(context, result);
    }

    private static async Task<FieldUpdateResult> TrySetKnownFieldAsync(
        string token,
        string label,
        string value,
        IListingReviewService reviews,
        CancellationToken ct)
    {
        string? path = FieldLabels.PathForLabel(label);
        FieldDefinition? field = path is null ? null : ListingFieldCatalog.Find(path);
        if (field is null || field.Kind is FieldKind.ObjectList or FieldKind.Json)
        {
            return FieldUpdateResult.UnknownField;
        }

        return await reviews.UpdateFieldAsync(token, field.Path, value, ct);
    }

    private static async Task<IResult> SetPublishContactAsync(
        string token,
        HttpRequest request,
        IReviewAccessService access,
        IListingReviewService reviews,
        CancellationToken ct)
    {
        if (!await HasSessionAsync(token, request, access, ct))
        {
            return Results.Unauthorized();
        }

        IFormCollection form = await request.ReadFormAsync(ct);
        bool publishContact = form[ValueFormKey].ToString() == CheckedFormValue;
        bool saved = await reviews.SetPublishContactAsync(token, publishContact, ct);
        return Html(ListingReviewPage.RenderFieldStatus(saved ? FieldUpdateResult.Saved : FieldUpdateResult.NotFound));
    }

    private static IResult RefreshOnSuccess(HttpContext context, FieldUpdateResult result)
    {
        if (result == FieldUpdateResult.Saved)
        {
            context.Response.Headers[HtmxRefreshHeader] = HtmxTrue;
        }

        return Html(ListingReviewPage.RenderFieldStatus(result));
    }

    private static async Task<IResult> ApproveAsync(
        string token,
        HttpRequest request,
        IReviewAccessService access,
        IListingReviewService reviews,
        CancellationToken ct)
    {
        if (!await HasSessionAsync(token, request, access, ct))
        {
            return Results.Unauthorized();
        }

        return Html(ListingReviewPage.RenderApproveStatus(await reviews.ApproveAsync(token, ct)));
    }

    private static async Task<IResult> ImageAsync(
        string token,
        int position,
        HttpRequest request,
        IReviewAccessService access,
        IListingReviewService reviews,
        IObjectStorage storage,
        CancellationToken ct)
    {
        if (!await HasSessionAsync(token, request, access, ct))
        {
            return Results.Unauthorized();
        }

        ListingReview? review = await reviews.GetByTokenAsync(token, ct);
        ListingImageRef? image = review?.Images.FirstOrDefault(candidate => candidate.Position == position);
        StoredObject? stored = image is null ? null : await storage.GetAsync(image.StorageKey, ct);
        return stored is null ? Results.NotFound() : Results.File(stored.Content, stored.ContentType);
    }

    private static Task<bool> HasSessionAsync(string token, HttpRequest request, IReviewAccessService access, CancellationToken ct) =>
        access.HasValidSessionAsync(token, request.Cookies[SessionCookieName], ct);

    private static CookieOptions SessionCookieOptions(string token, HttpRequest request) => new()
    {
        HttpOnly = true,
        Secure = request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = $"/review/{token}",
        MaxAge = ReviewAccessService.SessionValidity
    };

    private static IResult Html(string content) => Results.Content(content, HtmlContentType);
}
