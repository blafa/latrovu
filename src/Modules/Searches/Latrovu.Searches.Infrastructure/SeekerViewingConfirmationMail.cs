using System.Globalization;
using System.Net;
using Latrovu.BuildingBlocks.Infrastructure.Email;

namespace Latrovu.Searches.Infrastructure;

public static class SeekerViewingConfirmationMail
{
    private const string FontStack = "Outfit,Helvetica,Arial,sans-serif";

    public static string Subject(SeekerViewingConfirmation confirmation, SeekerTexts texts)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(texts);

        return Format(texts.ViewingSubject, confirmation.ListingTitle);
    }

    public static string Text(SeekerViewingConfirmation confirmation, SeekerTexts texts)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(texts);

        return $"{texts.Greeting}\n\n{Format(texts.ViewingIntro, confirmation.ListingTitle)}\n\n{When(confirmation)}\n\n"
            + $"{texts.ViewingNote}\n\n{texts.Closing}\n";
    }

    public static string Html(SeekerViewingConfirmation confirmation, SeekerTexts texts)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(texts);

        return $"<!doctype html><html lang=\"{Encode(confirmation.Language)}\"><body style=\"margin:0;padding:0;background:#F3F4F1;\">"
            + "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#F3F4F1;padding:32px 16px;\"><tr><td align=\"center\">"
            + $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:520px;background:#FFFFFF;border-radius:6px;font-family:{FontStack};color:#111418;font-size:15px;line-height:1.55;\">"
            + EmailBranding.HeaderRow
            + Row($"<p style=\"margin:0;\">{Encode(texts.Greeting)}</p><p style=\"margin:12px 0 0;\">{Encode(Format(texts.ViewingIntro, confirmation.ListingTitle))}</p>")
            + Row($"<div style=\"background:#EEF8D2;border-radius:4px;padding:14px 16px;font-weight:600;\">{Encode(When(confirmation))}</div>")
            + Row($"<p style=\"margin:0;color:#5E6660;font-size:13px;\">{Encode(texts.ViewingNote)}</p>")
            + Row($"<p style=\"margin:0 0 8px;white-space:pre-line;\">{Encode(texts.Closing)}</p>")
            + "</table></td></tr></table></body></html>";
    }

    private static string When(SeekerViewingConfirmation confirmation) =>
        SeekerViewingTime.Describe(confirmation.StartsAt, confirmation.EndsAt, confirmation.Language);

    private static string Format(string template, string value) => string.Format(CultureInfo.InvariantCulture, template, value);

    private static string Row(string content) => $"<tr><td style=\"padding:16px 32px 0;\">{content}</td></tr>";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
