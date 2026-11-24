namespace Latrovu.Listings.Infrastructure.Review.Web;

internal static class ReviewIcons
{
    public const string Check =
        "<svg viewBox=\"0 0 12 12\" aria-hidden=\"true\" focusable=\"false\">"
        + "<path d=\"M3.1 6.2 5 8.1l3.9-4.2\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" "
        + "stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg>";

    public static readonly string Home = Outline(
        "M5 12l-2 0l9 -9l9 9l-2 0",
        "M5 12v7a2 2 0 0 0 2 2h10a2 2 0 0 0 2 -2v-7",
        "M9 21v-6a2 2 0 0 1 2 -2h2a2 2 0 0 1 2 2v6");

    public static readonly string Tag = Outline(
        "M6.5 7.5a1 1 0 1 0 2 0a1 1 0 1 0 -2 0",
        "M3 6v5.172a2 2 0 0 0 .586 1.414l7.71 7.71a2.41 2.41 0 0 0 3.408 0l5.592 -5.592a2.41 2.41 0 0 0 0 -3.408l-7.71 -7.71a2 2 0 0 0 -1.414 -.586h-5.172a3 3 0 0 0 -3 3");

    public static readonly string MapPin = Outline(
        "M9 11a3 3 0 1 0 6 0a3 3 0 0 0 -6 0",
        "M17.657 16.657l-4.243 4.243a2 2 0 0 1 -2.827 0l-4.244 -4.243a8 8 0 1 1 11.314 0");

    public static readonly string Sparkles = Outline(
        "M16 18a2 2 0 0 1 2 2a2 2 0 0 1 2 -2a2 2 0 0 1 -2 -2a2 2 0 0 1 -2 2m0 -12a2 2 0 0 1 2 2a2 2 0 0 1 2 -2a2 2 0 0 1 -2 -2a2 2 0 0 1 -2 2m-7 12a6 6 0 0 1 6 -6a6 6 0 0 1 -6 -6a6 6 0 0 1 -6 6a6 6 0 0 1 6 6");

    public static readonly string User = Outline(
        "M8 7a4 4 0 1 0 8 0a4 4 0 0 0 -8 0",
        "M6 21v-2a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4v2");

    public static readonly string Text = Outline("M4 6l16 0", "M4 12l10 0", "M4 18l14 0");

    public static readonly string Photo = Outline(
        "M15 8h.01",
        "M3 6a3 3 0 0 1 3 -3h12a3 3 0 0 1 3 3v12a3 3 0 0 1 -3 3h-12a3 3 0 0 1 -3 -3v-12",
        "M3 16l5 -5c.928 -.893 2.072 -.893 3 0l5 5",
        "M14 14l1 -1c.928 -.893 2.072 -.893 3 0l3 3");

    public static readonly string Confirm = Outline("M5 12l5 5l10 -10");

    public static readonly string Alert = Outline("M3 12a9 9 0 1 0 18 0a9 9 0 0 0 -18 0", "M12 8v4", "M12 16h.01");

    public static readonly string Approved = Outline("M3 12a9 9 0 1 0 18 0a9 9 0 1 0 -18 0", "M9 12l2 2l4 -4");

    public const string Draft =
        "<svg class=\"icon\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" "
        + "stroke-linecap=\"round\" aria-hidden=\"true\" focusable=\"false\">"
        + "<circle cx=\"12\" cy=\"12\" r=\"8.5\" stroke-dasharray=\"3 3.2\"/></svg>";

    public static readonly string Derived = Outline("M6 6v6a3 3 0 0 0 3 3h10l-4 -4m0 8l4 -4");

    public static readonly string Pencil = Outline("M4 20h4l10.5 -10.5a2.828 2.828 0 1 0 -4 -4l-10.5 10.5v4", "M13.5 6.5l4 4");

    public static readonly string Cross = Outline("M18 6l-12 12", "M6 6l12 12");

    public static readonly string Plus = Outline("M12 5l0 14", "M5 12l14 0");

    public static readonly string Send = Outline("M10 14l11 -11", "M21 3l-6.5 18a.55 .55 0 0 1 -1 0l-3.5 -7l-7 -3.5a.55 .55 0 0 1 0 -1l18 -6.5");

    public static readonly string CollapseDown = Outline("M6 9l6 6l6 -6");

    public static readonly string Document = Outline(
        "M14 3v4a1 1 0 0 0 1 1h4",
        "M17 21h-10a2 2 0 0 1 -2 -2v-14a2 2 0 0 1 2 -2h7l5 5v11a2 2 0 0 1 -2 2",
        "M9 9l1 0",
        "M9 13l6 0",
        "M9 17l6 0");

    public static readonly string Eye = Outline(
        "M10 12a2 2 0 1 0 4 0a2 2 0 0 0 -4 0",
        "M21 12c-2.4 4 -5.4 6 -9 6c-3.6 0 -6.6 -2 -9 -6c2.4 -4 5.4 -6 9 -6c3.6 0 6.6 2 9 6");

    public static readonly string EyeOff = Outline(
        "M10.585 10.587a2 2 0 0 0 2.829 2.828",
        "M16.681 16.673a8.717 8.717 0 0 1 -4.681 1.327c-3.6 0 -6.6 -2 -9 -6c1.272 -2.12 2.712 -3.678 4.32 -4.674m2.86 -1.146a9.055 9.055 0 0 1 1.82 -.18c3.6 0 6.6 2 9 6c-.666 1.11 -1.379 2.067 -2.138 2.87",
        "M3 3l18 18");

    private static string Outline(params string[] paths) =>
        "<svg class=\"icon\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" "
        + "stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\" focusable=\"false\">"
        + string.Concat(paths.Select(path => $"<path d=\"{path}\"/>"))
        + "</svg>";
}
