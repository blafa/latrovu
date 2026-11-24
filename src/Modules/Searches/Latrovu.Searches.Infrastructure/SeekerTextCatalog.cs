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
            "Freundliche Grüsse\nIhr Latrovu-Team"),
        ["fr"] = new SeekerTexts(
            "Votre recherche de logement avec Latrovu",
            "Bonjour",
            "Voici ce que nous avons compris :",
            "Précisions possibles :",
            "Vos recherches :",
            "en pause",
            "Vous souhaitez modifier quelque chose ? Répondez simplement à ce message, avec vos propres mots. Avec « stop », vous terminez une recherche.",
            "Nous n’avons malheureusement pas bien compris votre message. Écrivez-nous simplement où et ce que vous cherchez, par exemple : « 3 pièces à Zurich jusqu’à 2500 francs ».",
            "Meilleures salutations\nL’équipe Latrovu"),
        ["it"] = new SeekerTexts(
            "La sua ricerca di casa con Latrovu",
            "Buongiorno",
            "Ecco cosa abbiamo capito:",
            "Possibili precisazioni:",
            "Le sue ricerche:",
            "in pausa",
            "Vuole cambiare qualcosa? Risponda semplicemente a questo messaggio, con parole sue. Con «stop» termina una ricerca.",
            "Purtroppo non abbiamo capito bene il suo messaggio. Ci scriva semplicemente dove e cosa cerca, per esempio: «3 locali a Zurigo fino a 2500 franchi».",
            "Cordiali saluti\nIl team di Latrovu"),
        ["en"] = new SeekerTexts(
            "Your home search with Latrovu",
            "Hello",
            "This is what we understood:",
            "Possible refinements:",
            "Your searches:",
            "paused",
            "Want to change something? Just reply to this message, in your own words. Write “stop” to end a search.",
            "Sorry, we did not quite understand your message. Just tell us where and what you are looking for, for example: “3 rooms in Zurich up to 2500 francs”.",
            "Kind regards\nThe Latrovu team")
    };

    public static SeekerTexts For(string language) =>
        Texts.TryGetValue(language, out SeekerTexts? texts) ? texts : Texts[FallbackLanguage];
}
