namespace Latrovu.Listings.Infrastructure.Viewing.Chat;

internal static class ChatReplyTexts
{
    private const string FallbackLanguage = "en";

    private static readonly Dictionary<string, (string Request, string Contact, string Provider)> Texts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["de"] = (
            "Wir haben Ihre Anfrage an den Anbieter weitergeleitet. Die Antwort erhalten Sie hier und per E-Mail.",
            "Kontaktdaten geben wir nicht heraus. Schreiben Sie Ihr Anliegen einfach hier, wir leiten es an den Anbieter weiter.",
            "der Anbieter"),
        ["fr"] = (
            "Nous avons transmis votre demande au fournisseur. Vous recevrez la réponse ici et par e-mail.",
            "Nous ne communiquons pas de coordonnées. Écrivez simplement votre demande ici, nous la transmettons au fournisseur.",
            "le fournisseur"),
        ["it"] = (
            "Abbiamo inoltrato la sua richiesta all'offerente. Riceverà la risposta qui e per e-mail.",
            "Non comunichiamo dati di contatto. Scriva semplicemente qui la sua richiesta, la inoltreremo all'offerente.",
            "l'offerente"),
        ["en"] = (
            "We have forwarded your request to the provider. You will receive the answer here and by email.",
            "We don't share contact details. Just write your request here and we'll pass it on to the provider.",
            "the provider")
    };

    public static string RequestForwarded(string language) => For(language).Request;

    public static string ContactViaLatrovu(string language) => For(language).Contact;

    public static string Provider(string language) => For(language).Provider;

    private static (string Request, string Contact, string Provider) For(string language) =>
        Texts.TryGetValue(language.Trim(), out (string Request, string Contact, string Provider) texts) ? texts : Texts[FallbackLanguage];
}
