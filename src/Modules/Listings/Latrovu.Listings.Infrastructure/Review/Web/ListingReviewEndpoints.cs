using System.Text.Json.Nodes;
using Latrovu.BuildingBlocks.Storage;
using Latrovu.Listings.Viewing.Chat;
using Latrovu.Listings.Viewing.Viewings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Latrovu.Listings.Infrastructure.Review.Web;

public static class ListingReviewEndpoints
{
    public const string SessionCookieName = "latrovu_review_session";
    private const string SessionCookiePath = "/review";
    private const string HtmlContentType = "text/html; charset=utf-8";
    private const string PathFormKey = "path";
    private const string ValueFormKey = "value";
    private const string CodeFormKey = "code";
    private const string LabelFormKey = "label";
    private const string HtmxRedirectHeader = "HX-Redirect";
    private const string HtmxRefreshHeader = "HX-Refresh";
    private const string HtmxRetargetHeader = "HX-Retarget";
    private const string HtmxReswapHeader = "HX-Reswap";
    private const string WholePageTarget = "body";
    private const string ReplaceContent = "innerHTML";
    private const string HtmxTrue = "true";
    private const string CheckedFormValue = "true";
    private const string DateFormKey = "date";
    private const string FromFormKey = "from";
    private const string ToFormKey = "to";
    private const string PlacesFormKey = "places";

    public static IEndpointRouteBuilder MapListingReview(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder review = app.MapGroup("/review/{token}");
        review.MapGet(string.Empty, ShowAsync);
        review.MapPost("/code", RequestCodeAsync);
        review.MapPost("/verify", VerifyCodeAsync);
        review.MapPost("/logout", LogoutAsync);
        review.MapPost("/fields", UpdateFieldAsync);
        review.MapPost("/confirm", ConfirmFieldAsync);
        review.MapPost("/details", AddCustomDetailAsync);
        review.MapPost("/publish-contact", SetPublishContactAsync);
        review.MapPost("/approve", ApproveAsync);
        review.MapPost("/withdraw", WithdrawAsync);
        review.MapPost("/knowledge/{entryId:long}/remove", RemoveKnowledgeAsync);
        review.MapPost("/viewings", AddViewingAsync);
        review.MapPost("/viewings/{slotId:long}/remove", RemoveViewingAsync);
        review.MapGet("/images/{position:int}", ImageAsync);
        return app;
    }

    private static async Task<IResult> ShowAsync(
        string token,
        HttpRequest request,
        IReviewAccessService access,
        IListingReviewService reviews,
        IListingKnowledgeStore knowledge,
        AgentViewingService viewings,
        CancellationToken ct)
    {
        if (!await HasSessionAsync(token, request, access, ct))
        {
            return Html(ReviewGatePage.RenderGate(token));
        }

        ListingReview? review = await reviews.GetByTokenAsync(token, ct);
        return review is null
            ? Results.NotFound()
            : Html(ListingReviewPage.Render(token, review, await knowledge.GetAsync(review.Id, ct), await viewings.GetAsync(token, ct)));
    }

    private static async Task<IResult> AddViewingAsync(
        string token,
        HttpRequest request,
        IReviewAccessService access,
        AgentViewingService viewings,
        CancellationToken ct)
    {
        if (!await HasSessionAsync(token, request, access, ct))
        {
            return Results.Unauthorized();
        }

        IFormCollection form = await request.ReadFormAsync(ct);
        SlotFormValues posted = new(form[DateFormKey].ToString(), form[FromFormKey].ToString(), form[ToFormKey].ToString(), form[PlacesFormKey].ToString());
        DateTimeOffset? startsAt = SwissTime.ToUtc(posted.Date, posted.From);
        DateTimeOffset? endsAt = SwissTime.ToUtc(posted.Date, posted.To);
        _ = int.TryParse(posted.Places.Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int places);
        ViewingResult result = startsAt is { } start && endsAt is { } end
            ? await viewings.AddAsync(token, start, end, places, ct)
            : (await viewings.GetAsync(token, ct)) with { Outcome = BookingOutcome.NotFound };
        SlotFormValues? keep = result.Outcome == BookingOutcome.NotFound ? posted : null;
        return result.Found ? Html(AgentViewingPanel.Render(token, result, keep)) : Results.NotFound();
    }

    private static async Task<IResult> RemoveViewingAsync(
        string token,
        long slotId,
        HttpRequest request,
        IReviewAccessService access,
        AgentViewingService viewings,
        CancellationToken ct)
    {
        if (!await HasSessionAsync(token, request, access, ct))
        {
            return Results.Unauthorized();
        }

        ViewingResult result = await viewings.RemoveAsync(token, slotId, ct);
        return result.Found ? Html(AgentViewingPanel.Render(token, result)) : Results.NotFound();
    }

    private static async Task<IResult> RemoveKnowledgeAsync(
        string token,
        long entryId,
        HttpRequest request,
        IReviewAccessService access,
        IListingReviewService reviews,
        IListingKnowledgeStore knowledge,
        CancellationToken ct)
    {
        if (!await HasSessionAsync(token, request, access, ct))
        {
            return Results.Unauthorized();
        }

        ListingReview? review = await reviews.GetByTokenAsync(token, ct);
        return review is not null && await knowledge.RemoveAsync(review.Id, entryId, ct) ? Html(string.Empty) : Results.NotFound();
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

        context.Response.Cookies.Append(SessionCookieName, result.SessionToken, SessionCookieOptions(context.Request));
        context.Response.Headers[HtmxRedirectHeader] = $"/review/{token}";
        return Html(string.Empty);
    }

    private static async Task<IResult> LogoutAsync(string token, HttpContext context, IReviewAccessService access, CancellationToken ct)
    {
        await access.LogoutAsync(context.Request.Cookies[SessionCookieName], ct);
        context.Response.Cookies.Delete(SessionCookieName, SessionCookieOptions(context.Request));
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

    private static Task<IResult> ApproveAsync(
        string token,
        HttpContext context,
        IReviewAccessService access,
        IListingReviewService reviews,
        CancellationToken ct) =>
        ChangeStatusAsync(token, context, access, () => reviews.ApproveAsync(token, ct), ct);

    private static async Task<IResult> WithdrawAsync(
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

        if (!await reviews.WithdrawAsync(token, ct))
        {
            return Html(ListingReviewPage.RenderStatusChange(false));
        }

        context.Response.Headers[HtmxRetargetHeader] = WholePageTarget;
        context.Response.Headers[HtmxReswapHeader] = ReplaceContent;
        return Html(WithdrawnNotice.Render());
    }

    private static async Task<IResult> ChangeStatusAsync(
        string token,
        HttpContext context,
        IReviewAccessService access,
        Func<Task<bool>> change,
        CancellationToken ct)
    {
        if (!await HasSessionAsync(token, context.Request, access, ct))
        {
            return Results.Unauthorized();
        }

        bool changed = await change();
        if (changed)
        {
            context.Response.Headers[HtmxRefreshHeader] = HtmxTrue;
        }

        return Html(ListingReviewPage.RenderStatusChange(changed));
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

    private static CookieOptions SessionCookieOptions(HttpRequest request) => new()
    {
        HttpOnly = true,
        Secure = request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = SessionCookiePath,
        MaxAge = ReviewAccessService.SessionValidity
    };

    private static IResult Html(string content) => Results.Content(content, HtmlContentType);
}
