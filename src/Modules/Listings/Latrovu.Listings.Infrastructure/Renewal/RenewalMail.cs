using System.Globalization;
using System.Net;
using Latrovu.BuildingBlocks.Infrastructure.Email;
using Latrovu.Listings.Renewal;

namespace Latrovu.Listings.Infrastructure.Renewal;

public static class RenewalMail
{
    private const string FontStack = "Outfit,Helvetica,Arial,sans-serif";
    private const string DateFormat = "dddd, d. MMMM yyyy";

    private static readonly CultureInfo SwissGerman = CultureInfo.GetCultureInfo("de-CH");
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");
    private static readonly string[] OnlineSince = ["seit einem Monat", "seit zwei Monaten", "seit drei Monaten"];

    public static string Subject(RenewalReminder reminder)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        return reminder.DeletionAt is { } deletion
            ? $"Letzte Erinnerung: «{reminder.Title}» wird am {Day(deletion)} gelöscht"
            : $"Ist Ihr Inserat noch aktuell? «{reminder.Title}»";
    }

    public static string Text(RenewalReminder reminder, string renewUrl, string reviewUrl)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        return $"Guten Tag\n\n{Intro(reminder)}\n\n{Deletion(reminder)}Ja, weiterhin aktiv: {renewUrl}\nInserat zurückziehen: {reviewUrl}\n\n"
            + "Freundliche Grüsse\nIhr latrovu-Team\n";
    }

    public static string Html(RenewalReminder reminder, string renewUrl, string reviewUrl)
    {
        ArgumentNullException.ThrowIfNull(reminder);

        return "<!doctype html><html lang=\"de-CH\"><body style=\"margin:0;padding:0;background:#F3F4F1;\">"
            + "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#F3F4F1;padding:32px 16px;\"><tr><td align=\"center\">"
            + $"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:520px;background:#FFFFFF;border-radius:6px;font-family:{FontStack};color:#111418;font-size:15px;line-height:1.55;\">"
            + EmailBranding.HeaderRow
            + Row($"<p style=\"margin:0;\">Guten Tag</p><p style=\"margin:12px 0 0;\">{Encode(Intro(reminder))}</p>")
            + (reminder.DeletionAt is null ? string.Empty
                : Row($"<div style=\"background:#FCF1DE;color:#7A4A07;border-radius:4px;padding:12px 16px;\">{Encode(Deletion(reminder).Trim())}</div>"))
            + Row($"<a href=\"{Encode(renewUrl)}\" style=\"display:inline-block;background:#B8E62E;color:#111418;font-weight:600;text-decoration:none;padding:12px 20px;border-radius:6px;margin:0 8px 8px 0;\">Ja, weiterhin aktiv</a>"
                + $"<a href=\"{Encode(reviewUrl)}\" style=\"display:inline-block;color:#111418;font-weight:600;text-decoration:none;padding:10px 18px;border:2px solid #111418;border-radius:6px;\">Inserat zurückziehen</a>")
            + Row("<p style=\"margin:0 0 8px;\">Freundliche Grüsse<br>Ihr latrovu-Team</p>")
            + "</table></td></tr></table></body></html>";
    }

    private static string Intro(RenewalReminder reminder) =>
        $"Ihr Inserat «{reminder.Title}» ist {OnlineSince[Math.Clamp(reminder.Number, 1, OnlineSince.Length) - 1]} auf Latrovu. Ist das Angebot noch aktuell?";

    private static string Deletion(RenewalReminder reminder) =>
        reminder.DeletionAt is { } deletion
            ? $"Ohne Bestätigung löschen wir das Inserat mit allen Daten am {Day(deletion)} automatisch.\n\n"
            : string.Empty;

    private static string Day(DateTimeOffset moment) => TimeZoneInfo.ConvertTime(moment, Zone).ToString(DateFormat, SwissGerman);

    private static string Row(string content) => $"<tr><td style=\"padding:16px 32px 0;\">{content}</td></tr>";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
