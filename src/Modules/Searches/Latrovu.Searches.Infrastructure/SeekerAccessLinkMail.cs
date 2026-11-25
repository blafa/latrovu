using System.Globalization;
using System.Net;
using Latrovu.BuildingBlocks.Infrastructure.Email;

namespace Latrovu.Searches.Infrastructure;

public static class SeekerAccessLinkMail
{
    private const string FontStack = "Outfit,Helvetica,Arial,sans-serif";

    public static string Subject(SeekerAccessLink link, SeekerTexts texts)
    {
        ArgumentNullException.ThrowIfNull(link);
        ArgumentNullException.ThrowIfNull(texts);

        return Format(texts.AccessSubject, link.ListingTitle);
    }

    public static string Text(SeekerAccessLink link, SeekerTexts texts, string url)
    {
        ArgumentNullException.ThrowIfNull(link);
        ArgumentNullException.ThrowIfNull(texts);

        return $"{texts.Greeting}\n\n{Format(texts.AccessIntro, link.ListingTitle)}\n\n{texts.AccessButton}: {url}\n\n{texts.AccessNote}\n\n{texts.Closing}\n";
    }

    public static string Html(SeekerAccessLink link, SeekerTexts texts, string url)
    {
        ArgumentNullException.ThrowIfNull(link);
        ArgumentNullException.ThrowIfNull(texts);

        return $"<!doctype html><html lang=\"{Encode(link.Language)}\"><body style=\"margin:0;padding:0;background:#F3F4F1;\">"
            + "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#F3F4F1;padding:32px 16px;\"><tr><td align=\"center\">"
            + $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:520px;background:#FFFFFF;border-radius:6px;font-family:{FontStack};color:#111418;font-size:15px;line-height:1.55;\">"
            + EmailBranding.HeaderRow
            + Row($"<p style=\"margin:0;\">{Encode(texts.Greeting)}</p><p style=\"margin:12px 0 0;\">{Encode(Format(texts.AccessIntro, link.ListingTitle))}</p>")
            + Row($"<a href=\"{Encode(url)}\" style=\"display:inline-block;background:#B8E62E;color:#111418;font-weight:600;text-decoration:none;padding:12px 20px;border-radius:6px;\">{Encode(texts.AccessButton)}</a>")
            + Row($"<p style=\"margin:0;color:#5E6660;font-size:13px;\">{Encode(texts.AccessNote)}</p>")
            + Row($"<p style=\"margin:0 0 8px;white-space:pre-line;\">{Encode(texts.Closing)}</p>")
            + "</table></td></tr></table></body></html>";
    }

    private static string Format(string template, string value) => string.Format(CultureInfo.InvariantCulture, template, value);

    private static string Row(string content) => $"<tr><td style=\"padding:16px 32px 0;\">{content}</td></tr>";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
