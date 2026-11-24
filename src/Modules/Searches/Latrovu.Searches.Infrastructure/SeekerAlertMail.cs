using System.Globalization;
using System.Net;
using Latrovu.BuildingBlocks.Infrastructure.Email;

namespace Latrovu.Searches.Infrastructure;

public static class SeekerAlertMail
{
    private const string FontStack = "Outfit,Helvetica,Arial,sans-serif";
    private const string FactSeparator = " · ";
    private static readonly CultureInfo SwissFormat = CultureInfo.GetCultureInfo("de-CH");

    public static string Facts(SeekerAlert alert, SeekerTexts texts)
    {
        ArgumentNullException.ThrowIfNull(alert);
        ArgumentNullException.ThrowIfNull(texts);

        List<string> facts = [];
        if (alert.Listing.Rooms is { } rooms)
        {
            facts.Add($"{rooms.ToString("0.#", CultureInfo.InvariantCulture)} {texts.Rooms}");
        }

        if (alert.Listing.LivingAreaM2 is { } area)
        {
            facts.Add($"{area.ToString("0.#", CultureInfo.InvariantCulture)} m²");
        }

        if (alert.Listing.PriceChf is { } price)
        {
            facts.Add($"CHF {price.ToString("N0", SwissFormat)}");
        }

        if (!string.IsNullOrWhiteSpace(alert.Listing.City))
        {
            facts.Add(alert.Listing.City);
        }

        return string.Join(FactSeparator, facts);
    }

    public static string Text(SeekerAlert alert, SeekerTexts texts, string url) =>
        $"{texts.Greeting}\n\n{Format(texts.AlertIntro, alert.SearchName)}\n\n{alert.Listing.Title}\n{Facts(alert, texts)}\n\n"
        + $"{texts.AlertButton}: {url}\n\n{texts.AlertNote}\n\n{texts.Closing}\n";

    public static string Html(SeekerAlert alert, SeekerTexts texts, string url) =>
        $"<!doctype html><html lang=\"{Encode(alert.Language)}\"><body style=\"margin:0;padding:0;background:#F3F4F1;\">"
        + "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#F3F4F1;padding:32px 16px;\"><tr><td align=\"center\">"
        + $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:520px;background:#FFFFFF;border-radius:6px;font-family:{FontStack};color:#111418;font-size:15px;line-height:1.55;\">"
        + EmailBranding.HeaderRow
        + Row($"<p style=\"margin:0;\">{Encode(texts.Greeting)}</p><p style=\"margin:12px 0 0;\">{Encode(Format(texts.AlertIntro, alert.SearchName))}</p>")
        + Row("<div style=\"background:#F6F7F4;border-radius:4px;padding:14px 16px;\">"
            + $"<p style=\"margin:0 0 4px;font-weight:600;font-size:16px;\">{Encode(alert.Listing.Title ?? string.Empty)}</p>"
            + $"<p style=\"margin:0;color:#5E6660;\">{Encode(Facts(alert, texts))}</p></div>")
        + Row($"<a href=\"{Encode(url)}\" style=\"display:inline-block;background:#B8E62E;color:#111418;font-weight:600;text-decoration:none;padding:12px 20px;border-radius:6px;\">{Encode(texts.AlertButton)}</a>")
        + Row($"<p style=\"margin:0;color:#5E6660;font-size:13px;\">{Encode(texts.AlertNote)}</p>")
        + Row($"<p style=\"margin:0 0 8px;white-space:pre-line;\">{Encode(texts.Closing)}</p>")
        + "</table></td></tr></table></body></html>";

    private static string Format(string template, string value) => string.Format(CultureInfo.InvariantCulture, template, value);

    private static string Row(string content) => $"<tr><td style=\"padding:16px 32px 0;\">{content}</td></tr>";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
