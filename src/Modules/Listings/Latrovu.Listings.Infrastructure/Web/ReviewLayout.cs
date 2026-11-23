using System.Net;
using System.Text;

namespace Latrovu.Listings.Infrastructure.Web;

internal static class ReviewLayout
{
    private const string HtmxScriptUrl = "https://unpkg.com/htmx.org@2.0.4/dist/htmx.min.js";
    private const string FontUrl = "https://fonts.googleapis.com/css2?family=Outfit:wght@400;600;700&display=swap";

    private const string Styles = """
        :root { --ink:#111418; --lime:#B8E62E; --bg:#F7F7F8; --line:#E2E4E8; --muted:#5B616B; }
        * { box-sizing:border-box; }
        body { margin:0; background:var(--bg); color:var(--ink); font:15px/1.5 Outfit, system-ui, sans-serif; }
        main { max-width:880px; margin:0 auto; padding:32px 20px 80px; }
        header { display:flex; justify-content:space-between; align-items:center; gap:16px; margin-bottom:24px; }
        h1 { font-size:24px; margin:0; } h2 { font-size:17px; margin:28px 0 10px; }
        .muted { color:var(--muted); font-size:13px; }
        .images { display:grid; grid-template-columns:repeat(auto-fill, minmax(150px, 1fr)); gap:8px; }
        .images img { width:100%; aspect-ratio:4/3; object-fit:cover; border-radius:8px; background:var(--line); }
        .field { display:grid; grid-template-columns:200px 1fr 90px; gap:12px; align-items:center;
                 padding:8px 12px; background:#fff; border:1px solid var(--line); border-left:4px solid transparent; }
        .field + .field { border-top:none; }
        .score-medium { border-left-color:#F2C230; } .score-low { border-left-color:#F08A24; }
        .score-confirmed { border-left-color:var(--lime); }
        input, select, textarea { width:100%; font:inherit; padding:6px 8px; border:1px solid var(--line); border-radius:6px; }
        .status { font-size:12px; color:var(--muted); }
        .gate { max-width:420px; margin:12vh auto 0; display:grid; gap:14px; }
        .gate form { display:grid; gap:10px; }
        .gate input { font-size:22px; letter-spacing:6px; text-align:center; }
        button { font:inherit; font-weight:600; background:var(--lime); color:var(--ink); border:none;
                 padding:12px 24px; border-radius:999px; cursor:pointer; }
        button.secondary { background:none; text-decoration:underline; padding:4px; font-weight:400; }
        """;

    public static void AppendHead(StringBuilder html, string title)
    {
        html.Append("<!doctype html><html lang=\"de-CH\"><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")
            .Append($"<title>{WebUtility.HtmlEncode(title)} – Latrovu</title>")
            .Append($"<link rel=\"stylesheet\" href=\"{FontUrl}\">")
            .Append($"<script src=\"{HtmxScriptUrl}\"></script>")
            .Append($"<style>{Styles}</style></head><body><main>");
    }

    public static void AppendEnd(StringBuilder html) => html.Append("</main></body></html>");
}
