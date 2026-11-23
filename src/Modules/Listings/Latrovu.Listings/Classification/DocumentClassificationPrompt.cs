namespace Latrovu.Listings.Classification;

public static class DocumentClassificationPrompt
{
    public const string Text = """
        Du klassifizierst ein Dokument aus dem Schweizer Immobilienbereich.
        Antworte ausschliesslich mit einem JSON-Objekt, ohne Erklärung und ohne Codeblock:
        { "documentType": "...", "reason": "kurze Begründung in einem Satz" }

        Werte für documentType:
        - "single_listing": Exposé, Verkaufs- oder Vermietungsdokumentation oder Inserat für genau ein Objekt.
        - "multi_unit_listing": Dokumentation mit mehreren angebotenen Einheiten, z.B. Wohnungsspiegel einer Erstvermietung.
        - "application_form": Anmeldeformular, Wohnungsbewerbung, Mietgesuch oder Mieterselbstauskunft, auch wenn Objektangaben vorausgefüllt sind.
        - "contract": Mietvertrag, Kaufvertrag, Allgemeine Bedingungen oder Reglement.
        - "other": alles andere, z.B. Grundriss ohne Angaben, Rechnung oder Brief.

        Ein Formular, das vom Interessenten ausgefüllt werden soll, ist nie ein Inserat.
        """;
}
