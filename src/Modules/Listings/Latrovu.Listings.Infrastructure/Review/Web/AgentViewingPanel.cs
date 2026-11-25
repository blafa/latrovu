using System.Globalization;
using System.Net;
using System.Text;
using Latrovu.Listings.Viewing.Viewings;

namespace Latrovu.Listings.Infrastructure.Review.Web;

internal static class AgentViewingPanel
{
    private const string Styles = """
        <style>
        .agent-viewings .card { background:var(--paper); border-radius:8px; padding:20px 24px; }
        .agent-viewings h2 { margin:0; font-size:17px; }
        .agent-viewings .hint { margin:4px 0 16px; font-size:13px; color:var(--muted); }
        .slot-form { display:flex; flex-wrap:wrap; align-items:flex-end; gap:8px; padding:12px; background:var(--well); border-radius:8px; }
        .slot-form label { display:grid; gap:3px; font-size:12px; color:var(--muted); }
        .slot-form input { font:inherit; font-size:14px; padding:7px 9px; border:1px solid var(--line); border-radius:6px; background:var(--paper); }
        .slot-form input.time, .slot-form input.places { width:72px; }
        .date-field { position:relative; display:flex; align-items:center; }
        .date-field input[name=date] { width:140px; padding-right:34px; }
        .slot-form .date-pick { position:absolute; right:4px; width:28px; height:28px; display:grid; place-items:center; padding:0; border:none; border-radius:4px; background:none; color:var(--muted); cursor:pointer; }
        .slot-form .date-pick:hover { background:var(--well); color:var(--ink); }
        .slot-form .date-pick .icon { width:16px; height:16px; }
        .date-native { position:absolute; right:0; bottom:0; width:1px; height:1px; opacity:0; pointer-events:none; }
        .slot-form .add { font:inherit; font-weight:600; font-size:14px; padding:8px 14px; border:none; border-radius:6px; background:var(--lime); cursor:pointer; }
        .slot-error { margin:8px 0 0; font-size:13px; color:#7A4A07; }
        .slot-grid { display:grid; grid-template-columns:repeat(auto-fill, minmax(230px, 1fr)); gap:10px; margin-top:16px; }
        .slot-card { position:relative; display:grid; gap:2px; padding:12px 14px; border:1px solid var(--line); border-radius:8px; }
        .slot-day { font-size:13px; color:var(--muted); }
        .slot-hours { font-size:17px; font-weight:600; }
        .slot-bar { height:4px; margin:8px 0 4px; border-radius:2px; background:var(--well); overflow:hidden; }
        .slot-bar span { display:block; height:100%; background:var(--lime); }
        .slot-count { font-size:12px; color:var(--muted); }
        .slot-names { font-size:12px; color:var(--ink); overflow-wrap:anywhere; }
        .slot-card .remove { position:absolute; top:8px; right:8px; width:28px; height:28px; display:grid; place-items:center; padding:0; border:none; border-radius:6px; background:none; color:var(--muted); cursor:pointer; }
        .slot-card .remove:hover { background:var(--well); color:var(--ink); }
        .slot-card .remove .icon { width:16px; height:16px; }
        .slots-empty { margin:16px 0 0; font-size:13px; color:var(--muted); }
        </style>
        """;

    public static string Render(string token, ViewingResult result, SlotFormValues? values = null)
    {
        StringBuilder html = new($"<section id=\"agent-viewings\" class=\"agent-viewings\" aria-labelledby=\"agent-viewings-title\">{Styles}<div class=\"card\">");
        html.Append("<h2 id=\"agent-viewings-title\">Besichtigungstermine</h2>")
            .Append("<p class=\"hint\">Interessenten buchen diese Termine direkt. Sie erhalten bei jeder Anmeldung eine E-Mail.</p>");
        AppendForm(html, token, values ?? SlotFormValues.Empty);
        if (result.Outcome == BookingOutcome.NotFound && result.Found)
        {
            html.Append("<p class=\"slot-error\" role=\"alert\">Bitte prüfen Sie Datum (z.B. 05.12.2026), Zeiten (z.B. 10 oder 10:30) und Plätze (1 bis 50). Der Termin muss in der Zukunft liegen, das Ende nach dem Beginn.</p>");
        }

        html.Append(result.Slots.Count == 0
            ? "<p class=\"slots-empty\">Noch keine Termine.</p>"
            : $"<div class=\"slot-grid\">{string.Join(string.Empty, result.Slots.Select(slot => Card(token, slot)))}</div>");
        return html.Append("</div></section>").ToString();
    }

    private static string Card(string token, ViewingSlot slot)
    {
        int percent = slot.Capacity == 0 ? 0 : (int)Math.Round(100.0 * slot.Booked / slot.Capacity);
        string names = slot.Attendees.Count == 0 ? string.Empty : $"<span class=\"slot-names\">{Encode(string.Join(", ", slot.Attendees))}</span>";
        string remove = slot.Booked == 0
            ? $"<button class=\"remove\" aria-label=\"Termin entfernen\" hx-post=\"/review/{Encode(token)}/viewings/{slot.Id}/remove\" "
                + $"hx-target=\"#agent-viewings\" hx-swap=\"outerHTML\" hx-disabled-elt=\"this\">{ReviewIcons.Trash}</button>"
            : string.Empty;
        return "<div class=\"slot-card\">"
            + $"<span class=\"slot-day\">{Encode(SwissTime.Day(slot))}</span>"
            + $"<span class=\"slot-hours\">{Encode(SwissTime.Hours(slot))}</span>"
            + $"<div class=\"slot-bar\" aria-hidden=\"true\"><span style=\"width:{percent.ToString(CultureInfo.InvariantCulture)}%\"></span></div>"
            + $"<span class=\"slot-count\">{slot.Booked} von {slot.Capacity} angemeldet</span>{names}{remove}</div>";
    }

    private static void AppendForm(StringBuilder html, string token, SlotFormValues values) =>
        html.Append($"<form class=\"slot-form\" novalidate hx-post=\"/review/{Encode(token)}/viewings\" hx-target=\"#agent-viewings\" hx-swap=\"outerHTML\">")
            .Append($"<label>Datum<span class=\"date-field\"><input name=\"date\" value=\"{Encode(values.Date)}\" placeholder=\"TT.MM.JJJJ\" inputmode=\"numeric\" autocomplete=\"off\">")
            .Append($"<button type=\"button\" class=\"date-pick\" aria-label=\"Kalender öffnen\">{ReviewIcons.Calendar}</button>")
            .Append("<input type=\"date\" class=\"date-native\" tabindex=\"-1\" aria-hidden=\"true\"></span></label>")
            .Append($"<label>Von<input class=\"time\" name=\"from\" value=\"{Encode(values.From)}\" placeholder=\"10:00\" inputmode=\"numeric\" autocomplete=\"off\"></label>")
            .Append($"<label>Bis<input class=\"time\" name=\"to\" value=\"{Encode(values.To)}\" placeholder=\"10:30\" inputmode=\"numeric\" autocomplete=\"off\"></label>")
            .Append($"<label>Plätze<input class=\"places\" name=\"places\" value=\"{Encode(values.Places)}\" inputmode=\"numeric\" autocomplete=\"off\"></label>")
            .Append("<button type=\"submit\" class=\"add\">Termin hinzufügen</button></form>");

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
