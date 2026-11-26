using System.Net;
using System.Text;
using Latrovu.Listings.Viewing.Chat;
using Latrovu.Listings.Viewing.Viewings;

namespace Latrovu.Listings.Infrastructure.Review.Web;

public static class ListingReviewPage
{
    private const string PageTitle = "Inserat prüfen";

    private const string TabStyles = """
        <style>
        .tabs-bar { position:sticky; top:var(--topbar); z-index:4; background:var(--canvas); border-bottom:1px solid var(--line); }
        .tabs-inner { max-width:1360px; margin:0 auto; padding:0 24px; display:flex; gap:4px; overflow-x:auto; scrollbar-width:none; }
        .tabs-inner::-webkit-scrollbar { display:none; }
        .tab { flex:none; display:inline-flex; align-items:center; gap:6px; padding:12px 14px 10px; border:none; border-bottom:2px solid transparent;
               background:none; font-size:14px; font-weight:500; color:var(--muted); }
        .tab[aria-selected="true"] { color:var(--ink); border-bottom-color:var(--ink); }
        .tab .icon { width:18px; height:18px; }
        .tab-count { min-width:18px; padding:0 6px; border-radius:9px; background:var(--lime); color:#173404; font-size:11px; font-weight:600; line-height:18px; text-align:center; }
        .tab-panel { max-width:1360px; margin:0 auto; padding:16px 24px 48px; }
        @media (max-width: 960px) {
            .pane.inline { grid-column:1 / -1; flex-basis:100%; width:100%; margin:2px 0 8px; padding:14px; background:var(--paper);
                           border:1px solid var(--line); border-radius:8px; }
            #pane-intro { display:none !important; }
            aside.context:not(:has(.pane:not([hidden]):not(#pane-intro))) { display:none; }
        }
        @media (max-width: 760px) {
            .topbar { height:auto; }
            .topbar-inner { flex-wrap:wrap; gap:8px 12px; padding:10px 16px; }
            .topbar .brand { border-right:none; padding-right:0; }
            .listing-id { order:3; flex-basis:100%; padding-top:8px; border-top:1px solid var(--well); }
            .listing-id h1 { font-size:15px; }
            body { padding-bottom:calc(68px + env(safe-area-inset-bottom)); }
            .tabs-bar { position:fixed; top:auto; bottom:0; left:0; right:0; z-index:20; background:var(--paper); border-bottom:none;
                        border-top:1px solid var(--line); padding-bottom:env(safe-area-inset-bottom); }
            .tabs-inner { padding:0; gap:0; }
            .tab { position:relative; flex:1; flex-direction:column; gap:3px; padding:8px 4px 7px; font-size:11px; border-bottom:none;
                   border-top:2px solid transparent; }
            .tab[aria-selected="true"] { border-top-color:var(--ink); }
            .tab .icon { width:22px; height:22px; }
            .tab-count { position:absolute; top:4px; left:calc(50% + 6px); min-width:16px; line-height:16px; font-size:10px; }
        }
        </style>
        """;

    private const string TabScript = """
        <script>
        function showTab(name) {
            const panels = [...document.querySelectorAll('[data-tab-panel]')];
            const target = panels.find(panel => panel.dataset.tabPanel === name) ? name : 'details';
            panels.forEach(panel => panel.hidden = panel.dataset.tabPanel !== target);
            document.querySelectorAll('[data-tab]').forEach(tab => tab.setAttribute('aria-selected', String(tab.dataset.tab === target)));
            const url = new URL(location.href);
            if (target === 'details') { url.searchParams.delete('tab'); } else { url.searchParams.set('tab', target); }
            history.replaceState(null, '', url);
        }
        document.addEventListener('click', event => { const tab = event.target.closest('[data-tab]'); if (tab) { showTab(tab.dataset.tab); } });
        document.addEventListener('click', event => {
            const pick = event.target.closest('.date-pick');
            if (!pick) { return; }
            event.preventDefault();
            const native = pick.parentElement.querySelector('.date-native');
            native.min = new Date().toISOString().slice(0, 10);
            native.showPicker ? native.showPicker() : native.click();
        });
        function fillPickedDate(event) {
            if (!event.target.matches('.date-native') || !event.target.value) { return; }
            const [year, month, day] = event.target.value.split('-');
            event.target.parentElement.querySelector('input[name=date]').value = `${day}.${month}.${year}`;
        }
        document.addEventListener('change', fillPickedDate);
        document.addEventListener('input', fillPickedDate);
        showTab(new URLSearchParams(location.search).get('tab') || 'details');
        </script>
        """;

    private const string PageScript = """
        <script>
        function select(id) {
            const pane = document.getElementById('pane-' + id) || document.getElementById('pane-intro');
            document.querySelectorAll('.pane').forEach(other => other.hidden = other !== pane);
            document.querySelectorAll('.spec, .chip').forEach(item => {
                const key = item.dataset.select ?? item.querySelector('[data-select]')?.dataset.select;
                item.classList.toggle('selected', key === id);
            });
            history.replaceState(null, '', pane.id === 'pane-intro' ? location.pathname : '#' + id);
            return pane;
        }
        const firstSpec = document.querySelector('.summary [data-select]:not(.ghost)');
        document.addEventListener('click', event => {
            const open = event.target.closest('[data-dialog-open]');
            if (open) { document.getElementById(open.dataset.dialogOpen)?.showModal(); }
            const close = event.target.closest('[data-dialog-close]');
            if (close) { close.closest('dialog')?.close(); }
            if (event.target.tagName === 'DIALOG') { event.target.close(); }
        });
        const narrowScreen = window.matchMedia('(max-width: 960px)');
        function placePane(trigger) {
            const spec = trigger.closest('.spec, .chip');
            const pane = document.getElementById('pane-' + trigger.dataset.select);
            if (narrowScreen.matches && spec && pane && !pane.hidden && pane.previousElementSibling === spec) {
                select('intro');
                return;
            }
            const shown = select(trigger.dataset.select);
            if (narrowScreen.matches && spec) {
                spec.after(shown);
                shown.classList.add('inline');
                spec.scrollIntoView({ behavior: 'smooth', block: 'start' });
                return;
            }
            document.querySelector('aside.context').append(shown);
            shown.classList.remove('inline');
            if (narrowScreen.matches) { shown.scrollIntoView({ behavior: 'smooth' }); }
        }
        select(location.hash.slice(1) || (narrowScreen.matches ? 'intro' : firstSpec?.dataset.select) || 'intro');

        const thumbs = [...document.querySelectorAll('.thumb')];
        let photoIndex = 0;
        function showPhoto(index) {
            if (thumbs.length === 0) { return; }
            photoIndex = (index + thumbs.length) % thumbs.length;
            const photo = document.getElementById('hero-photo');
            photo.src = thumbs[photoIndex].dataset.full;
            photo.alt = 'Bild ' + (photoIndex + 1) + ' von ' + thumbs.length;
            document.getElementById('photo-counter').textContent = (photoIndex + 1) + ' / ' + thumbs.length;
            thumbs.forEach((thumb, position) => thumb.classList.toggle('active', position === photoIndex));
        }

        function syncList(list) {
            const items = [...list.querySelectorAll('tbody tr')].map(row => {
                const item = {};
                row.querySelectorAll('[data-key]').forEach(input => {
                    const raw = input.value.trim();
                    let value = null;
                    if (raw !== '' && input.dataset.kind === 'number') {
                        value = Number(raw.replace(/['’\s]/g, '').replace(',', '.'));
                        if (Number.isNaN(value)) { value = null; }
                    } else if (raw !== '' && input.dataset.kind === 'boolean') {
                        value = raw === 'true';
                    } else if (raw !== '') {
                        value = raw;
                    }
                    item[input.dataset.key] = value;
                });
                return item;
            }).filter(item => Object.values(item).some(value => value !== null));
            const hidden = list.querySelector('input[type=hidden]');
            hidden.value = JSON.stringify(items);
            hidden.dispatchEvent(new Event('change', { bubbles: true }));
        }

        document.addEventListener('click', event => {
            const target = event.target.closest('[data-select]');
            if (target) {
                placePane(target);
            }
            const thumb = event.target.closest('.thumb');
            if (thumb) { showPhoto(thumbs.indexOf(thumb)); }
            const step = event.target.closest('[data-step]');
            if (step) { showPhoto(photoIndex + Number(step.dataset.step)); }
            const add = event.target.closest('[data-add-row]');
            if (add) {
                const list = add.closest('[data-list]');
                list.querySelector('tbody').append(list.querySelector('template').content.cloneNode(true));
            }
            const remove = event.target.closest('[data-remove-row]');
            if (remove) {
                const list = remove.closest('[data-list]');
                remove.closest('tr').remove();
                syncList(list);
            }
        });

        document.addEventListener('change', event => {
            const list = event.target.closest('[data-list]');
            if (list && event.target.type !== 'hidden') { syncList(list); }
        });

        document.addEventListener('keydown', event => {
            if (event.target.closest('input, select, textarea') || document.getElementById('pane-images')?.hidden !== false) { return; }
            if (event.key === 'ArrowLeft') { showPhoto(photoIndex - 1); }
            if (event.key === 'ArrowRight') { showPhoto(photoIndex + 1); }
        });
        </script>
        """;

    public static string Render(string token, ListingReview review, IReadOnlyList<KnowledgeEntry> knowledge, ViewingResult viewings)
    {
        ReviewModel model = ReviewModel.From(token, review);

        StringBuilder html = new();
        ReviewLayout.AppendHead(html, PageTitle);
        AppendTopBar(html, model);
        AppendTabs(html, viewings.Slots.Sum(slot => slot.Booked), knowledge.Count);
        html.Append("<main class=\"workspace\" id=\"tab-details\" data-tab-panel=\"details\">");
        ReviewSummary.Append(html, model);
        ReviewContextPane.Append(html, model);
        html.Append("</main>")
            .Append("<div class=\"tab-panel\" data-tab-panel=\"viewings\" hidden>").Append(AgentViewingPanel.Render(token, viewings)).Append("</div>")
            .Append("<div class=\"tab-panel\" data-tab-panel=\"answers\" hidden>").Append(ReviewKnowledgePanel.Render(token, knowledge)).Append("</div>")
            .Append(TabScript)
            .Append(PageScript);
        ReviewLayout.AppendEnd(html);
        return html.ToString();
    }

    public static string RenderFieldStatus(FieldUpdateResult result) => result switch
    {
        FieldUpdateResult.Saved => "Gespeichert",
        FieldUpdateResult.InvalidValue => "Ungültiger Wert",
        FieldUpdateResult.UnknownField => "Unbekanntes Feld",
        _ => "Nicht gefunden"
    };

    public static string RenderStatusChange(bool changed) =>
        changed ? "Gespeichert" : "Der Status konnte nicht geändert werden";

    private static void AppendTabs(StringBuilder html, int bookings, int answers) =>
        html.Append(TabStyles)
            .Append("<nav class=\"tabs-bar\" aria-label=\"Bereiche\"><div class=\"tabs-inner\" role=\"tablist\">")
            .Append(Tab("details", ReviewIcons.Document, "Angaben", 0))
            .Append(Tab("viewings", ReviewIcons.Calendar, "Besichtigungen", bookings))
            .Append(Tab("answers", ReviewIcons.Conversation, "Fragen & Antworten", answers))
            .Append("</div></nav>");

    private static string Tab(string name, string icon, string label, int count) =>
        $"<button type=\"button\" class=\"tab\" role=\"tab\" data-tab=\"{name}\" aria-selected=\"false\">{icon}{Encode(label)}"
        + (count > 0 ? $"<span class=\"tab-count\">{count}</span>" : string.Empty)
        + "</button>";

    private static void AppendTopBar(StringBuilder html, ReviewModel model)
    {
        string title = ListingValues.Text(model.Data, "title") ?? model.Review.FileName ?? PageTitle;
        html.Append($"<header class=\"topbar\"><div class=\"topbar-inner\">{SiteAssets.LogoImage(SiteAssets.CompactLogoHeight)}")
            .Append($"<div class=\"listing-id\"><h1 title=\"{Encode(title)}\">{Encode(title)}</h1>")
            .Append($"<span class=\"listing-address\">{Encode(ListingValues.Address(model.Data))}</span></div>")
            .Append("<div class=\"actions\"><span id=\"approve-status\" class=\"status\" aria-live=\"polite\"></span>")
            .Append($"<div class=\"approve\">{StatusAction(model)}{StatusChip(model.Review)}</div>")
            .Append($"<button type=\"button\" class=\"secondary\" hx-post=\"/review/{Encode(model.Token)}/logout\" title=\"Sitzung auf diesem Gerät beenden\">Abmelden</button>")
            .Append("</div></div></header>");
    }

    private static string StatusAction(ReviewModel model)
    {
        string token = Encode(model.Token);
        return model.Review.IsApproved
            ? $"<button type=\"button\" class=\"ghost caution\" data-dialog-open=\"withdraw-dialog\">{ReviewIcons.EyeOff}Zurückziehen</button>"
                + "<dialog id=\"withdraw-dialog\" class=\"dialog\" aria-labelledby=\"withdraw-title\">"
                + "<h2 id=\"withdraw-title\">Inserat zurückziehen und löschen?</h2>"
                + "<p>Das Inserat wird mit allen Bildern, Fragen, Besichtigungsterminen und Links für Suchende sofort gelöscht. "
                + "Das lässt sich nicht rückgängig machen. Möchten Sie es später wieder anbieten, senden Sie das Exposé einfach erneut.</p>"
                + "<div class=\"dialog-actions\"><button type=\"button\" class=\"ghost\" data-dialog-close>Abbrechen</button>"
                + $"<button class=\"caution-solid\" hx-post=\"/review/{token}/withdraw\" hx-target=\"#approve-status\" hx-disabled-elt=\"this\">"
                + $"<span class=\"spinner\" aria-hidden=\"true\"></span>{ReviewIcons.EyeOff}Ja, zurückziehen und löschen</button></div></dialog>"
            : $"<button class=\"primary\" hx-post=\"/review/{token}/approve\" hx-target=\"#approve-status\">{ReviewIcons.Confirm}Inserat freigeben</button>";
    }

    private static string StatusChip(ListingReview review) => review switch
    {
        { IsApproved: true } => $"<span class=\"state approved\">{ReviewIcons.Approved}Freigegeben</span>",
        { IsWithdrawn: true } => $"<span class=\"state withdrawn\">{ReviewIcons.Cross}Zurückgezogen</span>",
        _ => $"<span class=\"state\">{ReviewIcons.Draft}Entwurf</span>"
    };

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
