using System.Globalization;
using System.Net;
using Latrovu.BuildingBlocks.Infrastructure.Email;

namespace Latrovu.Searches.Infrastructure;

public static class SeekerViewingConfirmationMail
{
    private const string FontStack = "Outfit,Helvetica,Arial,sans-serif";
    private const string BookedHighlight = "#EEF8D2";
    private const string CancelledHighlight = "#FCF1DE";

    public static string Subject(SeekerViewingConfirmation confirmation, SeekerTexts texts)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(texts);

        return Format(SubjectTemplate(confirmation, texts), confirmation.ListingTitle);
    }

    public static string Text(SeekerViewingConfirmation confirmation, SeekerTexts texts)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(texts);

        return $"{texts.Greeting}\n\n{Intro(confirmation, texts)}\n\n{When(confirmation)}\n{Previous(confirmation, texts)}\n{Note(confirmation, texts)}\n\n{texts.Closing}\n";
    }

    public static string Html(SeekerViewingConfirmation confirmation, SeekerTexts texts)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        ArgumentNullException.ThrowIfNull(texts);

        string highlight = confirmation.IsCancellation ? CancelledHighlight : BookedHighlight;
        return $"<!doctype html><html lang=\"{Encode(confirmation.Language)}\"><body style=\"margin:0;padding:0;background:#F3F4F1;\">"
            + "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#F3F4F1;padding:32px 16px;\"><tr><td align=\"center\">"
            + $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:520px;background:#FFFFFF;border-radius:6px;font-family:{FontStack};color:#111418;font-size:15px;line-height:1.55;\">"
            + EmailBranding.HeaderRow
            + Row($"<p style=\"margin:0;\">{Encode(texts.Greeting)}</p><p style=\"margin:12px 0 0;\">{Encode(Intro(confirmation, texts))}</p>")
            + Row($"<div style=\"background:{highlight};border-radius:4px;padding:14px 16px;\"><p style=\"margin:0;font-weight:600;\">{Encode(When(confirmation))}</p>"
                + (confirmation.IsReschedule ? $"<p style=\"margin:4px 0 0;color:#5E6660;\">{Encode(Previous(confirmation, texts).Trim())}</p>" : string.Empty)
                + "</div>")
            + Row($"<p style=\"margin:0;color:#5E6660;font-size:13px;\">{Encode(Note(confirmation, texts))}</p>")
            + Row($"<p style=\"margin:0 0 8px;white-space:pre-line;\">{Encode(texts.Closing)}</p>")
            + "</table></td></tr></table></body></html>";
    }

    private static string SubjectTemplate(SeekerViewingConfirmation confirmation, SeekerTexts texts) =>
        confirmation.IsCancellation ? texts.CancelSubject : confirmation.IsReschedule ? texts.RescheduleSubject : texts.ViewingSubject;

    private static string Intro(SeekerViewingConfirmation confirmation, SeekerTexts texts) =>
        Format(
            confirmation.IsCancellation ? texts.CancelIntro : confirmation.IsReschedule ? texts.RescheduleIntro : texts.ViewingIntro,
            confirmation.ListingTitle);

    private static string Previous(SeekerViewingConfirmation confirmation, SeekerTexts texts) =>
        confirmation.IsReschedule
            ? $"{texts.PreviousLabel}: {SeekerViewingTime.Describe(confirmation.PreviousStartsAt!.Value, confirmation.PreviousEndsAt!.Value, confirmation.Language)}\n"
            : string.Empty;

    private static string Note(SeekerViewingConfirmation confirmation, SeekerTexts texts) =>
        confirmation.IsCancellation ? texts.CancelNote : texts.ViewingNote;

    private static string When(SeekerViewingConfirmation confirmation) =>
        SeekerViewingTime.Describe(confirmation.StartsAt, confirmation.EndsAt, confirmation.Language);

    private static string Format(string template, string value) => string.Format(CultureInfo.InvariantCulture, template, value);

    private static string Row(string content) => $"<tr><td style=\"padding:16px 32px 0;\">{content}</td></tr>";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
