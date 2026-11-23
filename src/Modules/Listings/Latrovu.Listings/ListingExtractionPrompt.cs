namespace Latrovu.Listings;

public static class ListingExtractionPrompt
{
    public static readonly string Text = Template.Replace(KeyFieldsPlaceholder, ListingKeyFields.AsBulletList());

    private const string KeyFieldsPlaceholder = "__KEY_FIELDS__";

    private const string Template = """
        Du extrahierst ALLE Informationen aus einem Schweizer Immobilien-Exposé.
        Ziel: Das Inserat muss jede Angabe zum angebotenen Objekt enthalten. Nichts weglassen, nichts erfinden.

        Regeln:
        - Antworte ausschliesslich mit einem gültigen JSON-Objekt, ohne Erklärung und ohne Codeblock.
        - Was nicht im Exposé steht, ist null.
        - Ja/Nein-Felder: true nur, wenn ausdrücklich vorhanden; false nur, wenn ausdrücklich verneint; sonst immer null. Nie raten.
        - Beschreibe nur das angebotene Objekt. Angaben zu anderen Einheiten der Überbauung gehören nicht in die Objektfelder.
        - Ignoriere Angaben zur Maklerfirma selbst (Sitz, Filialen, Handelsregister), Haftungsausschlüsse und Copyright-Vermerke.
          Die Kontaktperson und Kontaktdaten für dieses Objekt gehören aber immer vollständig in "contact".
        - Titel und Eckdaten-Tabelle haben Vorrang vor Fliesstext, wenn sich Angaben widersprechen.
        - Zahlen ohne Währung und Tausendertrennzeichen: "CHF 2'450.–" und "CHF 2.450.-" werden 2450, "3½ Zimmer" wird 3.5.
        - Datum als YYYY-MM-DD. Verfügbarkeit alternativ "sofort" oder "nach Vereinbarung".
        - "otherDetails" nur für Angaben, die in keinem Feld des Schemas Platz haben. Keine Werte doppelt erfassen.
        - Werte aus Auswahllisten exakt so schreiben wie angegeben.
        - "evidence": ein Objekt mit genau einem Eintrag pro Feld aus der folgenden Liste. Wert ist der exakte,
          wörtlich kopierte Textausschnitt aus dem Exposé, aus dem der Wert stammt, höchstens 100 Zeichen,
          sonst null. Nicht umformulieren, nicht zusammensetzen, nichts wiederholen.
          Bei abgeleiteten Werten (offerType, objectType, sale.ownershipType) die Textstelle zitieren,
          aus der du es ableitest, z.B. "Verkaufsdokumentation" oder "Wertquote 149/1000". Felder:
        __KEY_FIELDS__

        Begriffe:
        - rooms: Zimmerzahl des angebotenen Objekts.
        - livingAreaM2: Wohnfläche bzw. Nettowohnfläche (NWF). usableAreaM2 nur für reine Nutzfläche wie Keller oder Gewerbe.
        - grossFloorAreaM2: Bruttogeschossfläche (BGF), inklusive Wände. Nie in livingAreaM2 eintragen.
        - plotAreaM2: Grundstück, das allein zum Objekt gehört. Bei Stockwerkeigentum null; die Parzellengrösse dann in otherDetails.
        - ownershipType: "condominium" bei Stockwerkeigentum (StWE, Wertquote, STOWE-Kosten), "freehold" bei Alleineigentum mit eigenem Grundstück, "co_op" bei Genossenschaft.
        - condoFeesChfPerYear: STOWE-Kosten bzw. Nebenkosten des Stockwerkeigentums pro Jahr.
        - handoverCosts: Regelung der Handänderungskosten und Notariatskosten, z.B. "zulasten Käufer".
        - availability.availableFrom: Bezugs- bzw. Übergabetermin.
        - yearBuilt: Baujahr oder Jahr der Fertigstellung.
        - application: nur echte Hinweise zu Bewerbung oder Kaufabwicklung.
        - bathroomDetails: nur Angaben zu Bad, Dusche und WC.
        - documents.mentionedAttachments: nur Dokumente und Pläne, die im Exposé enthalten oder erwähnt sind.
        - description.summary: 2 bis 3 neutrale, sachliche Sätze in eigenen Worten, ohne Werbesprache.

        Schema:
        {
          "offerType": "rent" | "sale",
          "objectType": "apartment" | "studio" | "attic" | "maisonette" | "loft" | "terrace_apartment" | "furnished_apartment" | "room" | "house" | "parking" | "commercial" | "plot" | "other",
          "title": string,
          "referenceNumber": string,
          "location": {
            "street": string, "houseNumber": string, "zip": string, "city": string,
            "municipality": string, "canton": string (Kürzel, z.B. "ZH"), "district": string (z.B. "Kreis 4"),
            "distances": [{ "target": string, "meters": number, "minutes": number, "mode": "walk" | "transit" | "car" | "bike" }]
          },
          "size": {
            "rooms": number, "bedrooms": number, "bathrooms": number, "toilets": number,
            "livingAreaM2": number, "grossFloorAreaM2": number, "usableAreaM2": number, "plotAreaM2": number,
            "balconyAreaM2": number, "gardenAreaM2": number, "ceilingHeightM": number
          },
          "building": {
            "floor": number (Erdgeschoss = 0), "floorsInBuilding": number, "unitsInBuilding": number,
            "yearBuilt": number, "lastRenovationYear": number,
            "condition": "new" | "first_occupancy" | "renovated" | "good" | "needs_renovation"
          },
          "rent": {
            "netChf": number, "chargesChf": number, "chargesType": "akonto" | "pauschal", "grossChf": number,
            "depositChf": number, "depositMonths": number, "minimumRentalMonths": number,
            "rentType": "unlimited" | "limited", "limitedUntil": string
          },
          "sale": {
            "priceChf": number, "pricePerM2Chf": number, "ownershipType": "freehold" | "condominium" | "co_op",
            "valueQuota": string, "condoFeesChfPerYear": number, "renovationFundChf": number,
            "handoverCosts": string, "landRegistryNumber": string
          },
          "parking": [{
            "type": "garage" | "underground" | "carport" | "outdoor", "count": number,
            "priceChf": number, "pricePeriod": "month" | "once", "included": boolean
          }],
          "features": {
            "balcony": boolean, "terrace": boolean, "loggia": boolean, "garden": boolean, "elevator": boolean,
            "cellar": boolean, "atticStorage": boolean, "storageRoom": boolean,
            "ownWashingMachine": boolean, "sharedLaundry": boolean, "dishwasher": boolean,
            "fireplace": boolean, "floorHeating": boolean, "wheelchairAccessible": boolean, "stepFree": boolean,
            "petsAllowed": boolean, "smokingAllowed": boolean, "furnished": boolean,
            "view": string, "orientation": string, "kitchen": string, "bathroomDetails": string,
            "flooring": [string], "other": [string]
          },
          "energy": {
            "heatingType": "heat_pump" | "district" | "gas" | "oil" | "wood" | "pellets" | "electric" | "solar" | "other",
            "heatDistribution": "floor" | "radiators" | "other",
            "geakClass": string, "minergie": string, "solarPanels": boolean, "evCharging": boolean
          },
          "availability": { "availableFrom": string },
          "viewing": { "info": string, "dates": [string] },
          "application": { "instructions": string, "requiredDocuments": [string], "applicationUrl": string },
          "contact": { "company": string, "name": string, "role": string, "email": string, "phone": string, "website": string },
          "description": { "summary": string, "original": string (Objektbeschreibung aus dem Exposé, höchstens 1500 Zeichen) },
          "documents": { "hasFloorPlan": boolean, "mentionedAttachments": [string] },
          "otherDetails": [{ "label": string, "value": string }],
          "evidence": { "<feld>": string }
        }
        """;
}
