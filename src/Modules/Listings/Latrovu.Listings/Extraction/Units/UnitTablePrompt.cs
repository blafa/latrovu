namespace Latrovu.Listings.Extraction.Units;

public static class UnitTablePrompt
{
    public const string Text = """
        Du liest ein Schweizer Vermietungs- oder Verkaufsdokument, das mehrere Wohnungen anbietet
        (Erstvermietung, Neubau, Gesamtsanierung). Gib jede angebotene Wohnung als eigenen Eintrag in "units" zurück.

        Regeln:
        - Sind die Zeilen nummeriert ("Zeile 3: …"), gib pro Zeile genau eine Wohnung zurück, setze "row" auf die
          Zeilennummer und nimm alle Werte ausschliesslich aus dieser Zeile. Die Kopfzeile sagt, welche Spalte was bedeutet.
          Sonst "row" = null.
        - Nur Wohnungen, Studios und Häuser. Parkplätze, Kellerabteile, Gewerbeflächen und Bastelräume weglassen.
        - Jede Wohnung genau einmal, auch wenn sie im Dokument mehrfach vorkommt (Tabelle und Grundriss).
        - "label": die Bezeichnung aus dem Dokument, z.B. "Whg 2.1", "EG links", "1. OG Mitte". Fehlt eine,
          bilde sie aus Etage und Lage, z.B. "1. OG links".
        - "floor": Etage als Zahl. EG oder Hochparterre = 0, 1. OG = 1, Attika = oberstes Geschoss, UG = -1.
        - "rooms": Zimmerzahl als Zahl, 3½ = 3.5.
        - "livingAreaM2": Hauptnutzfläche oder Wohnfläche der Wohnung (HNF, Wohnfläche). Keine Aussenflächen.
        - Beträge in Franken als Zahl ohne Tausendertrennzeichen: 2'700.– = 2700.
          "netRentChf" Nettomiete, "chargesChf" Nebenkosten oder Akonto, "grossRentChf" Bruttomiete.
          Bei Verkauf "priceChf", sonst null.
        - "availableFrom": Bezugstermin als ISO-Datum (2025-04-01), wenn genannt, sonst null.
        - "status": "rented" wenn als vermietet markiert, "reserved" wenn reserviert, sonst "available".
          Ist unklar, ob die Wohnung noch frei ist, "available".
        - Unbekannte Werte immer null, nichts erfinden.
        """;
}
