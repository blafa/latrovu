using System.Globalization;
using System.Net;
using System.Text;
using Latrovu.Listings.Viewing.Chat;

namespace Latrovu.Listings.Infrastructure.Review.Web;

internal static class ReviewKnowledgePanel
{
    private static readonly CultureInfo SwissFormat = CultureInfo.GetCultureInfo("de-CH");

    private const string Styles = """
        <style>
        .knowledge { margin:0; }
        .knowledge-card { background:var(--paper); border-radius:8px; padding:20px 24px; }
        .knowledge h2 { margin:0; font-size:17px; }
        .knowledge-hint { margin:4px 0 14px; font-size:13px; color:var(--muted); }
        .knowledge ul { list-style:none; margin:0; padding:0; display:grid; gap:10px; }
        .knowledge li { display:grid; grid-template-columns:1fr auto; gap:4px 16px; padding:12px 14px; background:var(--well); border-radius:6px; }
        .knowledge-question { font-size:13px; color:var(--muted); white-space:pre-line; }
        .knowledge-answer { grid-column:1; font-size:15px; white-space:pre-line; }
        .knowledge-meta { grid-column:1; font-size:12px; color:var(--muted); }
        .knowledge-remove { grid-column:2; grid-row:1 / span 3; align-self:center; font:inherit; font-size:13px; padding:6px 12px;
                            border:1px solid var(--line); border-radius:6px; background:var(--paper); cursor:pointer; }
        </style>
        """;

    public static string Render(string token, IReadOnlyList<KnowledgeEntry> entries)
    {
        StringBuilder html = new(Styles);
        html.Append("<section class=\"knowledge\" aria-labelledby=\"knowledge-title\"><div class=\"knowledge-card\">")
            .Append("<h2 id=\"knowledge-title\">Antworten an Interessenten</h2>")
            .Append("<p class=\"knowledge-hint\">Diese Antworten nutzt der Chat für alle Interessenten, in deren Sprache. ")
            .Append("Entfernen Sie, was nicht mehr stimmt.</p>");
        if (entries.Count == 0)
        {
            html.Append("<p class=\"knowledge-hint\">Noch keine. Wenn Sie Fragen von Interessenten per E-Mail beantworten, ")
                .Append("erscheinen allgemeine Antworten hier.</p>");
        }
        else
        {
            html.Append("<ul>").AppendJoin(string.Empty, entries.Select(entry => Entry(token, entry))).Append("</ul>");
        }

        return html.Append("</div></section>").ToString();
    }

    private static string Entry(string token, KnowledgeEntry entry) =>
        "<li>"
        + $"<span class=\"knowledge-question\">{Encode(entry.Question)}</span>"
        + $"<span class=\"knowledge-answer\">{Encode(entry.Answer)}</span>"
        + $"<span class=\"knowledge-meta\">Beantwortet am {entry.CreatedAt.ToLocalTime().ToString("d. MMMM yyyy", SwissFormat)}</span>"
        + $"<button class=\"knowledge-remove\" hx-post=\"/review/{Encode(token)}/knowledge/{entry.Id}/remove\" "
        + "hx-target=\"closest li\" hx-swap=\"outerHTML\" hx-disabled-elt=\"this\">Entfernen</button>"
        + "</li>";

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
