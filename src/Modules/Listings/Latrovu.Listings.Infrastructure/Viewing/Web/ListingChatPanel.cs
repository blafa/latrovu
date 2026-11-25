using System.Net;
using System.Text;
using Latrovu.Listings.Infrastructure.Review.Web;
using Latrovu.Listings.Viewing.Chat;
using Latrovu.Listings.Viewing.Viewings;

namespace Latrovu.Listings.Infrastructure.Viewing.Web;

internal static class ListingChatPanel
{
    private const string UnknownFallback = "Das steht nicht im Exposé.";

    private static readonly string[] Suggestions =
    [
        "Sind Haustiere erlaubt?",
        "Gibt es einen Parkplatz?",
        "Wann kann ich besichtigen?"
    ];

    public const string Styles = """
        .chat { position:sticky; top:20px; display:flex; flex-direction:column; height:min(660px, calc(100vh - 40px));
                background:var(--paper); border-radius:8px; overflow:hidden; }
        .chat-header { display:flex; align-items:flex-start; gap:10px; padding:16px 18px 12px; border-bottom:1px solid var(--line); }
        .chat-close { display:none; flex:none; width:36px; height:36px; place-items:center; border:none; border-radius:50%; background:var(--well); color:var(--ink); cursor:pointer; }
        .chat-close .icon { width:20px; height:20px; }
        .chat-title { margin:0; font-size:16px; font-weight:600; }
        .chat-sub { margin:2px 0 0; font-size:12px; color:var(--muted); }
        .chat-messages { flex:1; overflow-y:auto; padding:14px 16px; display:flex; flex-direction:column; gap:8px; border-bottom:1px solid var(--line);
                         scrollbar-width:thin; scrollbar-color:var(--line) transparent; }
        .chat-messages::-webkit-scrollbar, .chat-form textarea::-webkit-scrollbar { width:6px; }
        .chat-messages::-webkit-scrollbar-thumb, .chat-form textarea::-webkit-scrollbar-thumb { background:var(--line); border-radius:3px; }
        .chat-messages::-webkit-scrollbar-button, .chat-form textarea::-webkit-scrollbar-button { display:none; }
        .chat-hint { margin:auto 0; text-align:center; font-size:13px; color:var(--muted); }
        .bubble { max-width:85%; padding:8px 12px; border-radius:14px; font-size:14px; line-height:1.45; white-space:pre-line; }
        .bubble.seeker { align-self:flex-end; background:#EEF8D2; border-bottom-right-radius:4px; }
        .bubble.answer { align-self:flex-start; background:var(--well); border-bottom-left-radius:4px; }
        .bubble-source { display:flex; align-items:center; gap:4px; margin-top:4px; font-size:11px; color:var(--muted); }
        .bubble-source .icon { width:12px; height:12px; }
        .typing { display:none; }
        .typing.htmx-request { display:inline-flex; gap:5px; align-items:center; padding:12px 14px; }
        .typing span { width:7px; height:7px; border-radius:50%; background:var(--ink); animation:blink 1.2s infinite both; }
        .typing span:nth-child(2) { animation-delay:0.2s; }
        .typing span:nth-child(3) { background:var(--lime); animation-delay:0.4s; }
        @keyframes blink { 0%, 80%, 100% { opacity:0.25; } 40% { opacity:1; } }
        .chat-note { margin:0; padding:0 16px 8px; font-size:12px; color:#7A4A07; }
        .forward { margin-top:8px; font:inherit; font-size:13px; font-weight:600; padding:6px 12px; border:1px solid var(--ink); border-radius:16px; background:var(--paper); cursor:pointer; }
        .forward[disabled] { opacity:0.6; cursor:progress; }
        .forward-note { margin:4px 0 0; font-size:11px; color:var(--muted); }
        .slot-chips { display:flex; flex-wrap:wrap; gap:6px; margin-top:8px; white-space:normal; }
        .slot-chip { font:inherit; font-size:13px; padding:6px 11px; border-radius:16px; border:1px solid var(--ink); background:var(--paper); color:var(--ink); cursor:pointer; }
        .slot-chip.mine { border-color:#EEF8D2; background:#EEF8D2; color:#27500A; cursor:default; }
        .slot-chip[disabled] { opacity:0.6; cursor:progress; }
        .chat-suggestions { display:flex; gap:6px; overflow-x:auto; padding:10px 12px 0; scrollbar-width:none; }
        .chat-suggestions::-webkit-scrollbar { display:none; }
        .suggestion { flex:none; font:inherit; font-size:13px; padding:6px 11px; border:1px solid var(--line); border-radius:16px; background:var(--paper); color:var(--ink); cursor:pointer; }
        .chat-form { display:flex; flex-direction:column; gap:6px; margin:10px 12px 12px; padding:10px 8px 8px 14px; border:1px solid var(--line);
                     border-radius:16px; background:var(--paper); }
        .chat-form:focus-within { border-color:var(--ink); }
        .chat-form textarea { width:100%; font:inherit; font-size:15px; line-height:1.45; padding:0 6px 0 0; border:none; outline:none; background:transparent;
                              resize:none; min-height:calc(1.45em * 3); max-height:220px; overflow-y:hidden;
                              scrollbar-width:thin; scrollbar-color:var(--line) transparent; }
        .composer-row { display:flex; align-items:center; justify-content:space-between; gap:8px; }
        .composer-hint { font-size:11px; color:var(--muted); }
        .chat-form button { flex:none; width:34px; height:34px; display:grid; place-items:center; border:none; border-radius:10px; background:var(--lime); color:var(--ink); cursor:pointer; }
        .chat-form button .icon { width:18px; height:18px; }
        .chat-form button[disabled] { opacity:0.6; cursor:progress; }
        @media (prefers-reduced-motion: reduce) { .typing span { animation:none; opacity:0.7; } }
        .unlock { margin:auto 0; display:grid; gap:8px; padding:16px; border-radius:12px; background:#EEF8D2; color:#27500A; font-size:14px; }
        .unlock p { margin:0; }
        .chat.locked { height:auto; }
        .chat.locked .chat-messages { flex:none; }
        .chat.locked .chat-form { opacity:0.5; }
        @media (max-width: 860px) {
            body { padding-bottom:140px; }
            .chat { position:fixed; left:0; right:0; bottom:0; top:auto; height:auto; z-index:40; border-radius:16px 16px 0 0;
                    box-shadow:0 -4px 20px rgba(17,20,24,0.10); padding-bottom:env(safe-area-inset-bottom); }
            .chat:not(.open) .chat-header, .chat:not(.open) .chat-messages, .chat:not(.open) .chat-note { display:none; }
            .chat:not(.open) .chat-form { flex-direction:row; align-items:center; padding:6px 6px 6px 14px; }
            .chat:not(.open) .chat-form textarea { flex:1; min-height:1.45em; }
            .composer-hint { display:none; }
            .chat.open { top:0; height:100dvh; border-radius:0; }
            .chat.open .chat-close { display:grid; }
            body.chat-open { overflow:hidden; }
            body:has(.chat.locked) { padding-bottom:0; }
            .chat.locked { position:static; height:auto; margin-top:24px; border-radius:8px; box-shadow:none; }
            .chat.locked .chat-header, .chat.locked .chat-messages { display:flex; }
        }
        """;

    public const string Script = """
        <script>
        let chatOpen = false;
        const mobileChat = window.matchMedia('(max-width: 860px)');
        function scrollChat() { const box = document.getElementById('chat-messages'); if (box) { box.scrollTop = box.scrollHeight; } }
        function applyChatState() {
            document.getElementById('chat')?.classList.toggle('open', chatOpen);
            document.body.classList.toggle('chat-open', chatOpen);
            if (chatOpen) { scrollChat(); }
        }
        function openChat() {
            if (!mobileChat.matches || chatOpen) { return; }
            chatOpen = true;
            history.pushState({ chat: true }, '');
            applyChatState();
        }
        function closeChat() { if (chatOpen) { history.back(); } }
        window.addEventListener('popstate', () => { if (chatOpen) { chatOpen = false; applyChatState(); } });
        document.addEventListener('keydown', event => { if (event.key === 'Escape') { closeChat(); } });
        function growQuestion(field) {
            field.style.height = 'auto';
            field.style.height = (field.scrollHeight + field.offsetHeight - field.clientHeight) + 'px';
            field.style.overflowY = field.scrollHeight > field.clientHeight + 1 ? 'auto' : 'hidden';
        }
        document.body.addEventListener('focusin', event => { if (event.target.matches('.chat-form textarea')) { openChat(); } });
        document.body.addEventListener('input', event => { if (event.target.matches('.chat-form textarea')) { growQuestion(event.target); } });
        document.body.addEventListener('click', event => {
            if (event.target.closest('.chat-close')) { closeChat(); return; }
            const suggestion = event.target.closest('.suggestion');
            if (!suggestion) { return; }
            const field = document.querySelector('.chat-form textarea');
            field.value = suggestion.textContent;
            openChat();
            field.form.requestSubmit();
        });
        document.body.addEventListener('keydown', event => {
            if (event.target.matches('.chat-form textarea') && event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
                event.preventDefault();
                if (event.target.value.trim()) { event.target.form.requestSubmit(); }
            }
        });
        let lastQuestion = '';
        document.body.addEventListener('htmx:beforeRequest', event => {
            const form = event.target.closest('.chat-form');
            const box = document.getElementById('chat-messages');
            if (!form || !box) { return; }
            const field = form.querySelector('textarea');
            const question = field.value.trim();
            if (!question) { event.preventDefault(); return; }
            lastQuestion = question;
            field.value = '';
            growQuestion(field);
            box.querySelector('.chat-hint')?.remove();
            box.querySelector('.chat-failed')?.remove();
            document.querySelector('.chat-suggestions')?.remove();
            const bubble = document.createElement('div');
            bubble.className = 'bubble seeker pending';
            bubble.textContent = question;
            box.insertBefore(bubble, document.getElementById('chat-typing'));
            scrollChat();
        });
        document.body.addEventListener('htmx:beforeSend', () => requestAnimationFrame(scrollChat));
        function chatFailed(event) {
            const box = document.getElementById('chat-messages');
            if (!event.target.closest('#chat') || !box) { return; }
            box.querySelectorAll('.bubble.pending').forEach(bubble => bubble.remove());
            const field = document.querySelector('.chat-form textarea');
            if (field && !field.value && lastQuestion) { field.value = lastQuestion; growQuestion(field); }
            const note = document.createElement('p');
            note.className = 'chat-note chat-failed';
            note.textContent = 'Das hat nicht geklappt. Bitte nochmals senden.';
            box.appendChild(note);
            scrollChat();
        }
        document.body.addEventListener('htmx:responseError', chatFailed);
        document.body.addEventListener('htmx:sendError', chatFailed);
        document.body.addEventListener('htmx:afterSwap', () => {
            applyChatState();
            scrollChat();
            if (!mobileChat.matches || chatOpen) { document.querySelector('.chat-form textarea')?.focus(); }
        });
        scrollChat();
        </script>
        """;

    public static string Render(string token, ChatResult result)
    {
        StringBuilder html = new("<section id=\"chat\" class=\"chat\" aria-label=\"Fragen zum Angebot\">");
        html.Append("<header class=\"chat-header\"><div style=\"flex:1\"><h2 class=\"chat-title\">Fragen zu diesem Angebot?</h2>")
            .Append("<p class=\"chat-sub\">Antworten aus dem Exposé, in Ihrer Sprache.</p></div>")
            .Append($"<button type=\"button\" class=\"chat-close\" aria-label=\"Chat schliessen\">{ReviewIcons.CollapseDown}</button></header>")
            .Append("<div class=\"chat-messages\" id=\"chat-messages\" aria-live=\"polite\">");
        if (result.Messages.Count == 0)
        {
            html.Append("<p class=\"chat-hint\">Fragen Sie in Ihrer Sprache, zum Beispiel:<br>«Sind Haustiere erlaubt?»</p>");
        }

        html.AppendJoin(string.Empty, result.Messages.Select((message, index) => Bubble(message, token, index == LastViewingsIndex(result) ? result.Slots : [])))
            .Append("<div class=\"bubble answer typing\" id=\"chat-typing\" aria-hidden=\"true\"><span></span><span></span><span></span></div>")
            .Append("</div>");
        AppendNote(html, result.Outcome);
        AppendSuggestions(html, result.Messages.Count);
        AppendForm(html, token);
        return html.Append("</section>").ToString();
    }

    public static string RenderLocked() =>
        "<section id=\"chat\" class=\"chat locked\" aria-label=\"Fragen zum Angebot\">"
        + "<header class=\"chat-header\"><div style=\"flex:1\"><h2 class=\"chat-title\">Fragen zu diesem Angebot?</h2>"
        + "<p class=\"chat-sub\">Antworten aus dem Exposé, in Ihrer Sprache.</p></div></header>"
        + "<div class=\"chat-messages\"><div class=\"unlock\"><strong>Fragen und Besichtigung mit Ihrem eigenen Link</strong>"
        + "<p>Dieser Link wurde mit Ihnen geteilt und ist nur zum Ansehen. Mit Ihrem persönlichen Link stellen Sie Fragen und buchen einen Termin.</p>"
        + "<button type=\"button\" class=\"unlock-open\" data-unlock>Persönlichen Link anfordern</button>"
        + "</div></div>"
        + "<div class=\"chat-form\" aria-hidden=\"true\"><textarea rows=\"1\" disabled placeholder=\"Nur mit persönlichem Link\"></textarea>"
        + $"<div class=\"composer-row\"><span class=\"composer-hint\"></span><button type=\"button\" disabled>{ReviewIcons.Send}</button></div></div>"
        + "</section>";

    private static void AppendSuggestions(StringBuilder html, int messageCount)
    {
        if (messageCount > 0)
        {
            return;
        }

        html.Append("<div class=\"chat-suggestions\">")
            .AppendJoin(string.Empty, Suggestions.Select(text => $"<button type=\"button\" class=\"suggestion\">{Encode(text)}</button>"))
            .Append("</div>");
    }

    private static int LastViewingsIndex(ChatResult result) =>
        result.Messages.ToList().FindLastIndex(message => message.Source == ChatMessage.ViewingsSource);

    private static string Bubble(ChatMessage message, string token, IReadOnlyList<ViewingSlot> slots)
    {
        if (message.Role == ChatMessage.SeekerRole)
        {
            return $"<div class=\"bubble seeker\">{Encode(message.Text)}</div>";
        }

        string text = string.IsNullOrWhiteSpace(message.Text) ? UnknownFallback : message.Text;
        return $"<div class=\"bubble answer\">{Encode(text)}<div class=\"bubble-source\">{SourceLabel(message)}</div>{SlotChips(token, slots)}{ForwardAction(message, token)}</div>";
    }

    private static string SlotChips(string token, IReadOnlyList<ViewingSlot> slots)
    {
        List<string> chips = slots
            .Where(slot => slot.BookedByMe || slot.FreePlaces > 0)
            .Select(slot => slot.BookedByMe
                ? $"<span class=\"slot-chip mine\">✓ {Encode(SwissTime.Describe(slot))}</span>"
                : $"<button class=\"slot-chip\" hx-post=\"/i/{Encode(token)}/viewings/{slot.Id}/book\" hx-target=\"#viewings\" hx-swap=\"outerHTML\" "
                    + $"hx-disabled-elt=\"this\">{Encode(SwissTime.Describe(slot))} buchen</button>")
            .ToList();
        return chips.Count == 0 ? string.Empty : $"<div class=\"slot-chips\">{string.Join(string.Empty, chips)}</div>";
    }

    private static string ForwardAction(ChatMessage message, string token) => message.CanBeForwarded
        ? $"<button class=\"forward\" hx-post=\"/i/{Encode(token)}/forward\" hx-vals='{{\"answerId\":\"{message.Id}\"}}' "
            + "hx-target=\"#chat\" hx-swap=\"outerHTML\" hx-disabled-elt=\"this\">Anbieter fragen</button>"
            + "<p class=\"forward-note\">Ohne Ihre Kontaktdaten. Die Antwort erhalten Sie hier und per E-Mail.</p>"
        : string.Empty;

    private static string SourceLabel(ChatMessage message) => message switch
    {
        { ForwardStatus: not null } => "An den Anbieter weitergeleitet",
        { Role: ChatMessage.AgentRole, Source: ChatMessage.TranslatedSource } => "Vom Anbieter beantwortet, übersetzt",
        { Role: ChatMessage.AgentRole } => "Vom Anbieter beantwortet",
        { Source: ChatMessage.ProviderSource } => "Vom Anbieter beantwortet",
        { Source: ChatMessage.ViewingsSource } => $"{ReviewIcons.Calendar}Besichtigungstermine",
        { Source: ChatMessage.BookingSource } => $"{ReviewIcons.Calendar}Besichtigung gebucht",
        { Source: ChatMessage.ExposeSource, Page: { } page } => $"{ReviewIcons.Document}Aus dem Exposé, Seite {page}",
        { Source: ChatMessage.ExposeSource } => $"{ReviewIcons.Document}Aus dem Exposé",
        { Source: ChatMessage.DataSource } => $"{ReviewIcons.Document}Aus den Angaben zum Inserat",
        { Source: ChatMessage.LatrovuSource } => "Hinweis von Latrovu",
        _ => "Nicht im Exposé"
    };

    private static void AppendNote(StringBuilder html, ChatOutcome outcome)
    {
        string? note = outcome switch
        {
            ChatOutcome.LimitReached => "Sie haben heute schon viele Fragen gestellt. Morgen können Sie weiterfragen.",
            ChatOutcome.InvalidQuestion => "Bitte schreiben Sie eine kurze Frage.",
            _ => null
        };
        if (note is not null)
        {
            html.Append($"<p class=\"chat-note\">{Encode(note)}</p>");
        }
    }

    private static void AppendForm(StringBuilder html, string token) =>
        html.Append($"<form class=\"chat-form\" hx-post=\"/i/{Encode(token)}/chat\" hx-target=\"#chat\" hx-swap=\"outerHTML\" ")
            .Append("hx-indicator=\"#chat-typing\" hx-disabled-elt=\"find button\">")
            .Append($"<textarea name=\"question\" rows=\"1\" maxlength=\"{ListingChatService.MaxQuestionLength}\" placeholder=\"Frage stellen …\" aria-label=\"Ihre Frage\"></textarea>")
            .Append("<div class=\"composer-row\"><span class=\"composer-hint\">Enter senden · Shift+Enter neue Zeile</span>")
            .Append($"<button type=\"submit\" aria-label=\"Frage senden\">{ReviewIcons.Send}</button></div></form>");

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
