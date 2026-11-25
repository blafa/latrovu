using System.Net;
using System.Text;
using Latrovu.Listings.Infrastructure.Review.Web;
using Latrovu.Listings.Viewing.Viewings;

namespace Latrovu.Listings.Infrastructure.Viewing.Web;

internal static class SeekerViewingPanel
{
    public const string Styles = """
        .viewings { margin:18px 0 4px; border-radius:10px; background:var(--paper); }
        .viewings summary { display:flex; align-items:center; gap:10px; padding:12px 14px; cursor:pointer; list-style:none; }
        .viewings summary::-webkit-details-marker { display:none; }
        .viewings summary .icon { flex:none; width:20px; height:20px; }
        .viewings-text { flex:1; display:grid; line-height:1.3; }
        .viewings-text strong { font-weight:600; }
        .viewings-text small { font-size:13px; color:var(--muted); }
        .viewings-action { flex:none; font-size:13px; font-weight:600; padding:7px 14px; border-radius:16px; background:var(--lime); color:var(--ink); }
        .viewings[open] .viewings-action { background:var(--well); }
        .viewings .unlock-action, .viewings[open] .unlock-action { font:inherit; font-size:13px; font-weight:600; border:none; background:var(--lime); cursor:pointer; }
        .viewings.booked { background:#EEF8D2; color:#27500A; }
        .viewings.booked .viewings-text small { color:#3B6D11; }
        .viewings.booked .viewings-action { background:none; padding:7px 4px; text-decoration:underline; font-weight:500; color:#27500A; }
        .viewings-list { padding:0 14px 10px; }
        .viewings-hint { margin:0 0 4px; font-size:12px; color:var(--muted); }
        .slot { display:flex; align-items:center; justify-content:space-between; gap:12px; padding:8px 0; border-top:1px solid var(--line); }
        .slot-when { font-weight:500; color:var(--ink); }
        .slot-free { font-size:12px; color:var(--muted); }
        .slot button { font:inherit; font-size:13px; font-weight:600; padding:6px 13px; border-radius:16px; cursor:pointer; border:1px solid var(--ink); background:var(--paper); color:var(--ink); }
        .slot button.book { background:var(--lime); border-color:var(--lime); }
        .slot button[disabled] { opacity:0.5; cursor:default; }
        .slot-mine { font-size:13px; font-weight:600; color:#3B6D11; margin-right:8px; }
        .viewings-note { margin:8px 0 0; font-size:12px; color:#7A4A07; }
        """;

    public static string Render(string token, ViewingResult result, bool locked = false)
    {
        if (result.Slots.Count == 0)
        {
            return "<section id=\"viewings\" hidden></section>";
        }

        ViewingSlot? mine = result.Slots.FirstOrDefault(slot => slot.BookedByMe);
        bool open = result.Outcome == BookingOutcome.Full;
        StringBuilder html = new($"<section id=\"viewings\"><details class=\"viewings{(mine is null ? string.Empty : " booked")}\"{(open ? " open" : string.Empty)}>");
        html.Append(Summary(result.Slots, mine, locked))
            .Append("<div class=\"viewings-list\">")
            .Append(locked
                ? string.Empty
                : "<p class=\"viewings-hint\">Bei der Buchung teilen wir dem Anbieter Ihre E-Mail-Adresse für diesen Termin mit.</p>")
            .AppendJoin(string.Empty, result.Slots.Select(slot => locked ? LockedSlot(slot) : Slot(token, slot)));
        if (open)
        {
            html.Append("<p class=\"viewings-note\">Dieser Termin ist leider schon voll. Bitte wählen Sie einen anderen.</p>");
        }

        return html.Append("</div></details></section>").ToString();
    }

    private static string Summary(IReadOnlyList<ViewingSlot> slots, ViewingSlot? mine, bool locked)
    {
        if (mine is not null)
        {
            return $"<summary>{ReviewIcons.Approved}<span class=\"viewings-text\"><strong>Besichtigung gebucht</strong>"
                + $"<small>{Encode(SwissTime.Describe(mine))}</small></span><span class=\"viewings-action\">ändern</span></summary>";
        }

        List<ViewingSlot> free = slots.Where(slot => slot.FreePlaces > 0).ToList();
        string detail = free.Count switch
        {
            0 => "Alle Termine sind ausgebucht",
            1 => $"1 Termin: {SwissTime.Day(free[0])}",
            _ => $"{free.Count} Termine ab {SwissTime.Day(free[0])}"
        };
        string action = locked
            ? "<button type=\"button\" class=\"viewings-action unlock-action\" data-unlock>Persönlichen Link anfordern</button>"
            : "<span class=\"viewings-action\">Termin wählen</span>";
        return $"<summary>{ReviewIcons.Calendar}<span class=\"viewings-text\"><strong>Besichtigung</strong><small>{Encode(detail)}</small></span>"
            + $"{action}</summary>";
    }

    private static string Slot(string token, ViewingSlot slot)
    {
        string encodedToken = Encode(token);
        string action = slot switch
        {
            { BookedByMe: true } => "<span class=\"slot-mine\">Gebucht ✓</span>"
                + $"<button hx-post=\"/i/{encodedToken}/viewings/cancel\" hx-target=\"#viewings\" hx-swap=\"outerHTML\" hx-disabled-elt=\"this\">Abmelden</button>",
            { FreePlaces: 0 } => "<button disabled>Ausgebucht</button>",
            _ => $"<button class=\"book\" hx-post=\"/i/{encodedToken}/viewings/{slot.Id}/book\" hx-target=\"#viewings\" hx-swap=\"outerHTML\" hx-disabled-elt=\"this\">Buchen</button>"
        };
        return SlotRow(slot, action);
    }

    private static string LockedSlot(ViewingSlot slot) =>
        SlotRow(slot, slot.FreePlaces == 0 ? "<span class=\"slot-free\">Ausgebucht</span>" : string.Empty);

    private static string SlotRow(ViewingSlot slot, string action)
    {
        string free = slot.FreePlaces == 1 ? "1 Platz frei" : $"{slot.FreePlaces} Plätze frei";
        return $"<div class=\"slot\"><div><div class=\"slot-when\">{Encode(SwissTime.Describe(slot))}</div><div class=\"slot-free\">{free}</div></div><div>{action}</div></div>";
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
