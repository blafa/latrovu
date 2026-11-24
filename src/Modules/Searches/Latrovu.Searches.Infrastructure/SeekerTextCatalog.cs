namespace Latrovu.Searches.Infrastructure;

public static class SeekerTextCatalog
{
    public const string FallbackLanguage = "en";

    private static readonly Dictionary<string, SeekerTexts> Texts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["de"] = new SeekerTexts(
            "Ihre Wohnungssuche bei Latrovu",
            "Guten Tag",
            "Das haben wir verstanden:",
            "Mögliche Verfeinerung:",
            "Ihre Suchen:",
            "pausiert",
            "Möchten Sie etwas ändern? Antworten Sie einfach auf diese Nachricht, in Ihren eigenen Worten. Mit «stop» beenden Sie eine Suche.",
            "Wir haben Ihre Nachricht leider nicht ganz verstanden. Schreiben Sie uns einfach, wo und was Sie suchen, zum Beispiel: «3 Zimmer in Zürich bis 2500 Franken».",
            "Freundliche Grüsse\nIhr Latrovu-Team",
            "Welches «{0}» meinen Sie: {1}?",
            "Den Ort «{0}» haben wir nicht gefunden. Können Sie ihn anders schreiben oder einen Ort in der Nähe nennen?",
            "Neues Angebot für Ihre Suche «{0}»",
            "Wir haben ein passendes Angebot für Ihre Suche «{0}» gefunden:",
            "Angebot ansehen",
            "Dieser Link ist 30 Tage gültig und nur für Sie bestimmt.",
            "Zimmer",
            "Beendet: {0}. Sie erhalten dafür keine Angebote mehr.",
            "Pausiert: {0}. Schreiben Sie uns, wenn es weitergehen soll.",
            "Wieder aktiv: {0}."),
        ["fr"] = new SeekerTexts(
            "Votre recherche de logement avec Latrovu",
            "Bonjour",
            "Voici ce que nous avons compris :",
            "Précisions possibles :",
            "Vos recherches :",
            "en pause",
            "Vous souhaitez modifier quelque chose ? Répondez simplement à ce message, avec vos propres mots. Avec « stop », vous terminez une recherche.",
            "Nous n’avons malheureusement pas bien compris votre message. Écrivez-nous simplement où et ce que vous cherchez, par exemple : « 3 pièces à Zurich jusqu’à 2500 francs ».",
            "Meilleures salutations\nL’équipe Latrovu",
            "Quel « {0} » voulez-vous dire : {1} ?",
            "Nous n’avons pas trouvé le lieu « {0} ». Pouvez-vous l’écrire autrement ou indiquer un lieu proche ?",
            "Nouvelle offre pour votre recherche « {0} »",
            "Nous avons trouvé une offre correspondant à votre recherche « {0} » :",
            "Voir l’offre",
            "Ce lien est valable 30 jours et vous est personnellement destiné.",
            "pièces",
            "Terminé : {0}. Vous ne recevrez plus d’offres pour cela.",
            "En pause : {0}. Écrivez-nous lorsque vous souhaitez reprendre.",
            "De nouveau actif : {0}."),
        ["it"] = new SeekerTexts(
            "La sua ricerca di casa con Latrovu",
            "Buongiorno",
            "Ecco cosa abbiamo capito:",
            "Possibili precisazioni:",
            "Le sue ricerche:",
            "in pausa",
            "Vuole cambiare qualcosa? Risponda semplicemente a questo messaggio, con parole sue. Con «stop» termina una ricerca.",
            "Purtroppo non abbiamo capito bene il suo messaggio. Ci scriva semplicemente dove e cosa cerca, per esempio: «3 locali a Zurigo fino a 2500 franchi».",
            "Cordiali saluti\nIl team di Latrovu",
            "Quale «{0}» intende: {1}?",
            "Non abbiamo trovato il luogo «{0}». Può scriverlo diversamente o indicare un luogo vicino?",
            "Nuova offerta per la sua ricerca «{0}»",
            "Abbiamo trovato un’offerta adatta alla sua ricerca «{0}»:",
            "Vedere l’offerta",
            "Questo link è valido 30 giorni ed è destinato solo a lei.",
            "locali",
            "Terminato: {0}. Non riceverà più offerte per questo.",
            "In pausa: {0}. Ci scriva quando vuole riprendere.",
            "Di nuovo attivo: {0}."),
        ["en"] = new SeekerTexts(
            "Your home search with Latrovu",
            "Hello",
            "This is what we understood:",
            "Possible refinements:",
            "Your searches:",
            "paused",
            "Want to change something? Just reply to this message, in your own words. Write “stop” to end a search.",
            "Sorry, we did not quite understand your message. Just tell us where and what you are looking for, for example: “3 rooms in Zurich up to 2500 francs”.",
            "Kind regards\nThe Latrovu team",
            "Which “{0}” do you mean: {1}?",
            "We could not find “{0}”. Could you spell it differently or name a place nearby?",
            "New listing for your search “{0}”",
            "We found a listing that matches your search “{0}”:",
            "View the listing",
            "This link is valid for 30 days and meant only for you.",
            "rooms",
            "Stopped: {0}. You will no longer receive listings for it.",
            "Paused: {0}. Write to us when you want to continue.",
            "Active again: {0}.")
    };

    public static SeekerTexts For(string language) =>
        Texts.TryGetValue(language, out SeekerTexts? texts) ? texts : Texts[FallbackLanguage];
}
