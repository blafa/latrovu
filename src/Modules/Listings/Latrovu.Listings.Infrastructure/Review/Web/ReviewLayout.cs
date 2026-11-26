using System.Net;
using System.Text;

namespace Latrovu.Listings.Infrastructure.Review.Web;

internal static class ReviewLayout
{
    private const string Styles = """
        :root { --ink:#111418; --paper:#FFFFFF; --canvas:#F3F4F1; --well:#F6F7F4; --line:#E1E4DD; --muted:#5E6660;
                --lime:#B8E62E; --lime-soft:#EEF8D2; --green:#4E8A1E; --green-soft:#E3F1D2;
                --amber:#E8A33D; --amber-soft:#FCF1DE; --amber-ink:#7A4A07; --topbar:64px; }
        * { box-sizing:border-box; }
        [hidden] { display:none !important; }
        body { margin:0; background:var(--canvas); color:var(--ink); font:13px/1.45 Outfit, system-ui, sans-serif; }
        img { display:block; }
        button { font:inherit; color:inherit; cursor:pointer; }
        h2, h3 { margin:0; }
        .topbar { position:sticky; top:0; z-index:5; height:var(--topbar); background:var(--paper); border-bottom:1px solid var(--line); }
        .topbar-inner { max-width:1360px; height:100%; margin:0 auto; padding:0 24px; display:flex; align-items:center; gap:14px; }
        .brand { font-weight:700; font-size:16px; letter-spacing:-0.02em; padding-right:14px; border-right:1px solid var(--line); }
        .listing-id { min-width:0; display:grid; }
        .listing-id h1 { margin:0; font-size:14px; font-weight:600; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
        .listing-address { font-size:12px; color:var(--muted); white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
        .approve { display:grid; gap:4px; }
        .state { display:inline-flex; align-items:center; gap:6px; font-size:11px; padding:2px 14px;
                 border-radius:4px; background:var(--canvas); color:var(--muted); }
        .state .icon { width:15px; height:15px; }
        .state.approved { background:var(--lime-soft); color:#27500A; }
        .state.withdrawn { background:var(--amber-soft); color:var(--amber-ink); }
        .actions { margin-left:auto; flex:none; display:flex; align-items:center; gap:12px; }
        .workspace { max-width:1360px; margin:0 auto; padding:16px 24px 48px; display:grid;
                     grid-template-columns:minmax(0, 7fr) minmax(0, 5fr); gap:16px; align-items:start; }
        .panel { background:var(--paper); border-radius:6px; }
        .summary-header { display:flex; align-items:flex-start; gap:16px; padding:18px 20px; border-bottom:1px solid var(--well); }
        .summary-header > div { flex:1; }
        .summary-header h2 { font-size:15px; font-weight:600; }
        .summary-header p { margin:2px 0 0; color:var(--muted); }
        .legend { display:flex; flex-wrap:wrap; gap:4px 16px; margin:10px 0 0; padding:0; list-style:none; font-size:12px; color:var(--muted); }
        .legend li { display:flex; align-items:center; gap:6px; }
        .group h3 { display:flex; align-items:center; gap:7px; font-size:12px; font-weight:600; color:var(--ink); margin:0 10px 8px; }
        .group h3 .icon { width:16px; height:16px; color:var(--muted); }
        .open-group h3 { color:var(--amber-ink); }
        .open-group h3 .icon { color:var(--amber); }
        .spec.open-spec { padding:0; background:var(--amber-soft); }
        .spec.open-spec:hover { background:#F9E8CC; }
        .spec.open-spec.selected { background:var(--amber-soft); box-shadow:inset 0 0 0 1.5px var(--amber); }
        .spec-main { display:grid; gap:1px; min-width:0; padding:7px 10px 4px; text-align:left; border:none; background:none; }
        .open-spec .spec-note { margin-left:0; }
        .spec-actions { display:flex; gap:6px; padding:2px 10px 9px; }
        .spec-actions .ghost { padding:3px 9px; font-size:12px; border-color:rgba(122, 74, 7, 0.2); }
        .spec-actions .ghost .icon { width:13px; height:13px; }
        .group { padding:14px 10px 10px; }
        .group + .group { border-top:1px solid var(--well); }
        .specs { display:grid; grid-template-columns:repeat(3, minmax(0, 1fr)); gap:6px; }
        .spec { display:grid; gap:1px; min-width:0; padding:7px 10px; text-align:left; border:none; border-radius:4px; background:var(--well); }
        .spec:hover { background:#EDEFE9; }
        .spec.selected { background:var(--lime-soft); }
        .spec.wide { grid-column:1 / -1; }
        .spec-label { display:flex; align-items:center; justify-content:space-between; gap:6px; min-width:0; font-size:12px; color:var(--muted); white-space:nowrap; }
        .spec-value { font-weight:500; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
        .spec-note { font-weight:400; font-size:11px; color:var(--amber-ink); margin-left:6px; }
        .mark { width:14px; height:14px; border-radius:50%; flex:none; display:inline-grid; place-items:center; }
        .mark svg { width:10px; height:10px; display:block; }
        .mark.found { background:var(--green-soft); color:var(--green); }
        .mark.confirmed { background:var(--lime); color:#27500A; }
        .mark.open { background:var(--amber); width:8px; height:8px; margin:0 3px; }
        .mark.derived { background:#E6E9E3; color:#5E6660; }
        .mark.ai { background:#ECE8F4; color:#5B4B8A; }
        .mark .icon { stroke-width:2.6; }
        .icon { width:16px; height:16px; flex:none; }
        .chips { display:flex; flex-wrap:wrap; gap:6px; margin:0 0 8px; }
        .chip { display:inline-flex; align-items:center; gap:5px; padding:6px 10px 6px 8px; border:none; border-radius:4px;
                background:var(--well); font-weight:500; }
        .chip .icon { width:14px; height:14px; }
        .chip.yes .icon { color:var(--green); }
        .chip.no { color:var(--muted); font-weight:400; }
        .chip.no span { text-decoration:line-through; text-decoration-color:#B9BEB3; }
        .chip:hover { background:#EDEFE9; }
        .chip.selected { background:var(--lime-soft); }
        .add-detail { display:grid; grid-template-columns:auto minmax(0, 1fr) minmax(0, 1fr) auto; gap:8px; align-items:center;
                      padding:12px 20px; border-top:1px solid var(--well); background:var(--well); border-radius:0 0 6px 6px; }
        .add-title { font-weight:600; font-size:12px; padding-right:4px; }
        .add-detail .status { grid-column:1 / -1; min-height:0; }
        .add-detail .status:empty { display:none; }
        .publish-toggle { display:grid; grid-template-columns:auto 1fr; gap:4px 12px; align-items:center; margin:4px 0 8px;
                          padding:10px 12px; background:var(--well); border-radius:4px; }
        .switch { display:flex; align-items:center; gap:10px; cursor:pointer; font-weight:500; }
        .switch input { position:absolute; opacity:0; width:1px; height:1px; }
        .switch-track { position:relative; flex:none; width:30px; height:18px; border-radius:9px; background:#C9CDC4; transition:background 0.15s; }
        .switch-track::after { content:""; position:absolute; top:2px; left:2px; width:14px; height:14px; border-radius:50%;
                               background:var(--paper); transition:transform 0.15s; }
        .switch input:checked + .switch-track { background:var(--green); }
        .switch input:checked + .switch-track::after { transform:translateX(12px); }
        .switch input:focus-visible + .switch-track { outline:2px solid var(--ink); outline-offset:2px; }
        .toggle-hint { grid-column:1 / -1; margin:0; font-size:12px; color:var(--muted); }
        .switch-icon { display:inline-flex; color:var(--muted); }
        .switch input:checked ~ .switch-icon.off, .switch input:not(:checked) ~ .switch-icon.on { display:none; }
        .switch input:checked ~ .switch-icon.on { color:var(--green); }
        @media (prefers-reduced-motion: reduce) { .switch-track, .switch-track::after { transition:none; } }
        .context { position:sticky; top:calc(var(--topbar) + 16px); padding:18px; }
        .pane { display:grid; gap:12px; }
        .pane h2 { font-size:16px; font-weight:600; }
        .pane-title { display:flex; align-items:center; gap:10px; }
        .pane-kicker { margin:0 0 -10px; font-size:11px; color:var(--muted); }
        .intro { margin:0; color:var(--muted); max-width:42ch; }
        .editor-field { display:grid; gap:4px; }
        .control { display:flex; align-items:center; gap:8px; min-width:0; }
        .unit { color:var(--muted); font-size:12px; white-space:nowrap; }
        .evidence { margin:0; padding:14px 16px; background:var(--well); border-radius:4px; }
        .evidence figcaption { display:flex; align-items:center; gap:6px; font-size:11px; color:var(--muted); margin-bottom:6px; }
        .evidence figcaption .icon { width:14px; height:14px; }
        .evidence blockquote { margin:0; color:var(--muted); line-height:1.6; }
        .evidence mark { background:var(--green-soft); color:var(--ink); padding:1px 3px; border-radius:3px; font-weight:500; }
        .evidence.missing { background:var(--amber-soft); color:var(--amber-ink); }
        .evidence.neutral { color:var(--muted); }
        .viewer { position:relative; aspect-ratio:4/3; background:var(--well); border-radius:4px; display:grid; place-items:center; overflow:hidden; }
        .viewer img { max-width:100%; max-height:100%; object-fit:contain; }
        .nav { position:absolute; top:50%; transform:translateY(-50%); width:30px; height:30px; padding:0; border-radius:50%;
               border:1px solid var(--line); background:rgba(255, 255, 255, 0.92); font-size:18px; line-height:1; }
        .nav.prev { left:8px; } .nav.next { right:8px; }
        .counter { position:absolute; right:8px; bottom:8px; font-size:11px; padding:1px 6px; border-radius:4px; background:rgba(255, 255, 255, 0.92); color:var(--muted); }
        .thumbs { display:flex; gap:4px; overflow-x:auto; scrollbar-width:thin; }
        .thumb { flex:0 0 48px; height:48px; padding:0; border:2px solid transparent; border-radius:4px; background:var(--well); overflow:hidden; }
        .thumb img { width:100%; height:100%; object-fit:cover; }
        .thumb.active { border-color:var(--ink); }
        .custom-detail { display:grid; gap:10px; }
        .custom-detail label { display:grid; gap:3px; font-size:11px; color:var(--muted); }
        input, select, textarea { width:100%; font:inherit; color:inherit; padding:6px 9px; border:1px solid var(--line); border-radius:4px; background:var(--paper); }
        textarea { min-height:110px; resize:vertical; }
        input:focus, select:focus, textarea:focus, button:focus-visible { outline:2px solid var(--ink); outline-offset:1px; }
        .status { font-size:11px; color:var(--muted); min-height:16px; }
        .object-list { width:100%; overflow-x:auto; }
        .object-list table { width:100%; border-collapse:collapse; }
        .object-list th { text-align:left; font-size:11px; font-weight:400; color:var(--muted); padding:0 6px 4px 0; }
        .object-list td { padding:0 6px 6px 0; }
        .object-list td:last-child { width:32px; }
        .visually-hidden { position:absolute; width:1px; height:1px; overflow:hidden; clip:rect(0 0 0 0); white-space:nowrap; }
        .primary { display:inline-flex; align-items:center; gap:6px; border:none; border-radius:4px; padding:8px 14px; font-weight:600; background:var(--lime); color:var(--ink); }
        .primary:hover { filter:brightness(0.96); }
        .ghost { display:inline-flex; align-items:center; gap:6px; border:1px solid var(--line); border-radius:4px; padding:5px 11px; background:var(--paper); white-space:nowrap; }
        .primary .icon, .ghost .icon { width:15px; height:15px; }
        .ghost:hover { border-color:var(--muted); }
        .ghost.caution { color:var(--amber-ink); border-color:rgba(122, 74, 7, 0.3); }
        .ghost.caution:hover { background:var(--amber-soft); border-color:var(--amber); }
        .dialog { width:min(420px, calc(100vw - 32px)); border:none; border-radius:8px; padding:22px 22px 18px; color:var(--ink);
                  box-shadow:0 18px 48px rgba(17, 20, 24, 0.18); }
        .dialog::backdrop { background:rgba(17, 20, 24, 0.35); backdrop-filter:blur(2px); }
        .dialog h2 { margin:0 0 6px; font-size:16px; font-weight:600; }
        .dialog p { margin:0; color:var(--muted); }
        .dialog-actions { display:flex; justify-content:flex-end; gap:8px; margin-top:18px; }
        .caution-solid { display:inline-flex; align-items:center; gap:6px; border:none; border-radius:4px; padding:6px 12px;
                         font-weight:600; background:var(--amber); color:#3D2604; }
        .caution-solid .icon { width:15px; height:15px; }
        .caution-solid:hover { filter:brightness(0.95); }
        .pane > .ghost { justify-self:start; }
        .secondary { border:none; background:none; padding:2px 0; text-decoration:underline; color:var(--muted); }
        .remove { border:none; background:none; padding:3px 7px; border-radius:4px; color:var(--muted); }
        .remove:hover { background:var(--canvas); color:var(--ink); }
        .gate { max-width:420px; margin:12vh auto 0; display:grid; justify-items:start; gap:14px; padding:0 20px; }
        .gate h1 { font-size:20px; margin:0; }
        .gate form { display:grid; gap:12px; }
        .code-form { width:max-content; }
        .code-form .primary { padding:10px 14px; }
        .gate .primary { justify-content:center; }
        .secondary { display:inline-flex; align-items:center; gap:6px; }
        .spinner { display:none; width:13px; height:13px; border:2px solid currentColor; border-right-color:transparent;
                   border-radius:50%; animation:spin 0.7s linear infinite; }
        .htmx-request .spinner, .htmx-request.spinner { display:inline-block; }
        button[disabled] { opacity:0.7; cursor:progress; }
        @keyframes spin { to { transform:rotate(360deg); } }
        @media (prefers-reduced-motion: reduce) { .spinner { animation-duration:2s; } }
        .code-inputs { display:flex; gap:8px; }
        .code-inputs input { width:48px; height:56px; padding:0; text-align:center; font-size:24px; font-weight:600;
                             border-radius:6px; caret-color:var(--ink); }
        .code-inputs input:focus { outline:2px solid var(--ink); outline-offset:0; }
        @media (max-width: 960px) {
            .topbar-inner, .workspace { padding-left:16px; padding-right:16px; }
            .workspace { grid-template-columns:1fr; }
            .context { position:static; max-height:none; }
            .specs { grid-template-columns:repeat(2, minmax(0, 1fr)); }
        }
        @media (max-width: 600px) {
            .specs { grid-template-columns:1fr; }
            .add-detail { grid-template-columns:1fr; }
            .summary-header { flex-direction:column; }
            .brand { display:none; }
        }
        """;

    public static void AppendHead(StringBuilder html, string title)
    {
        html.Append("<!doctype html><html lang=\"de-CH\"><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">")
            .Append($"<title>{WebUtility.HtmlEncode(title)} – latrovu</title>")
            .Append(SiteAssets.HeadLinks)
            .Append($"<script src=\"{SiteAssets.HtmxScript}\"></script>")
            .Append($"<style>{SiteAssets.FontFaces}{Styles}</style></head><body>");
    }

    public static void AppendEnd(StringBuilder html) => html.Append("</body></html>");
}
