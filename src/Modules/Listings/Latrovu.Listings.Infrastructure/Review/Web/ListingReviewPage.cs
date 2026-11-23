using System.Net;
using System.Text;

namespace Latrovu.Listings.Infrastructure.Review.Web;

public static class ListingReviewPage
{
    private const string PageTitle = "Inserat prüfen";
    private const string ApprovedStatus = "approved";

    private const string PageScript = """
        <script>
        function select(id) {
            const pane = document.getElementById('pane-' + id) || document.getElementById('pane-intro');
            document.querySelectorAll('.pane').forEach(other => other.hidden = other !== pane);
            document.querySelectorAll('.spec, .chip').forEach(item => item.classList.toggle('selected', item.dataset.select === id));
            history.replaceState(null, '', pane.id === 'pane-intro' ? location.pathname : '#' + id);
            return pane;
        }
        const firstSpec = document.querySelector('.spec, .chip');
        select(location.hash.slice(1) || firstSpec?.dataset.select || 'intro');

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
                const pane = select(target.dataset.select);
                if (window.matchMedia('(max-width: 960px)').matches) { pane.scrollIntoView({ behavior: 'smooth' }); }
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

    public static string Render(string token, ListingReview review)
    {
        ReviewModel model = ReviewModel.From(token, review);

        StringBuilder html = new();
        ReviewLayout.AppendHead(html, PageTitle);
        AppendTopBar(html, model);
        html.Append("<main class=\"workspace\">");
        ReviewSummary.Append(html, model);
        ReviewContextPane.Append(html, model);
        html.Append("</main>").Append(PageScript);
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

    public static string RenderApproveStatus(bool approved) =>
        approved ? "Inserat freigegeben" : "Das Inserat ist bereits freigegeben";

    private static void AppendTopBar(StringBuilder html, ReviewModel model)
    {
        bool approved = model.Review.Status == ApprovedStatus;
        string title = ListingValues.Text(model.Data, "title") ?? model.Review.FileName ?? PageTitle;
        html.Append("<header class=\"topbar\"><div class=\"topbar-inner\"><span class=\"brand\">latrovu</span>")
            .Append($"<div class=\"listing-id\"><h1 title=\"{Encode(title)}\">{Encode(title)}</h1>")
            .Append($"<span class=\"listing-address\">{Encode(ListingValues.Address(model.Data))}</span></div>")
            .Append("<div class=\"actions\"><span id=\"approve-status\" class=\"status\" aria-live=\"polite\"></span>")
            .Append("<div class=\"approve\">")
            .Append($"<button class=\"primary\" hx-post=\"/review/{Encode(model.Token)}/approve\" hx-target=\"#approve-status\">{ReviewIcons.Confirm}Inserat freigeben</button>")
            .Append($"<span class=\"state{(approved ? " approved" : string.Empty)}\">Status: {(approved ? "freigegeben" : "Entwurf")}</span>")
            .Append("</div></div></div></header>");
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
