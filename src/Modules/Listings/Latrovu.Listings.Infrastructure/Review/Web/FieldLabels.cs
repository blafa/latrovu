namespace Latrovu.Listings.Infrastructure.Review.Web;

internal static class FieldLabels
{
    private const string SquareMeters = "m²";
    private const string Francs = "CHF";
    private const string Months = "Monate";

    public static readonly IReadOnlySet<string> LongTextPaths = new HashSet<string>
    {
        "description.summary",
        "description.original",
        "application.instructions",
        "viewing.info"
    };

    private static readonly Dictionary<string, string> Labels = new()
    {
        ["title"] = "Titel", ["offerType"] = "Angebot", ["objectType"] = "Objektart", ["referenceNumber"] = "Referenz",
        ["location.street"] = "Strasse", ["location.houseNumber"] = "Hausnummer", ["location.zip"] = "PLZ",
        ["location.city"] = "Ort", ["location.municipality"] = "Gemeinde", ["location.canton"] = "Kanton",
        ["location.district"] = "Quartier", ["location.distances"] = "Distanzen",
        ["size.rooms"] = "Zimmer", ["size.bedrooms"] = "Schlafzimmer", ["size.bathrooms"] = "Badezimmer",
        ["size.toilets"] = "WC", ["size.livingAreaM2"] = "Wohnfläche", ["size.grossFloorAreaM2"] = "Bruttogeschossfläche",
        ["size.usableAreaM2"] = "Nutzfläche", ["size.plotAreaM2"] = "Grundstück", ["size.balconyAreaM2"] = "Balkon",
        ["size.gardenAreaM2"] = "Garten", ["size.ceilingHeightM"] = "Raumhöhe",
        ["building.floor"] = "Etage", ["building.floorsInBuilding"] = "Etagen im Haus",
        ["building.unitsInBuilding"] = "Wohnungen im Haus", ["building.yearBuilt"] = "Baujahr",
        ["building.lastRenovationYear"] = "Letzte Renovation", ["building.condition"] = "Zustand",
        ["rent.netChf"] = "Nettomiete", ["rent.chargesChf"] = "Nebenkosten", ["rent.chargesType"] = "Nebenkosten als",
        ["rent.grossChf"] = "Bruttomiete", ["rent.depositChf"] = "Kaution", ["rent.depositMonths"] = "Kaution in Monatsmieten",
        ["rent.minimumRentalMonths"] = "Mindestmietdauer", ["rent.rentType"] = "Mietdauer", ["rent.limitedUntil"] = "Befristet bis",
        ["sale.priceChf"] = "Kaufpreis", ["sale.pricePerM2Chf"] = "Preis pro m²", ["sale.ownershipType"] = "Eigentumsform",
        ["sale.valueQuota"] = "Wertquote", ["sale.condoFeesChfPerYear"] = "STOWE-Kosten pro Jahr",
        ["sale.renovationFundChf"] = "Erneuerungsfonds", ["sale.handoverCosts"] = "Handänderungskosten",
        ["sale.landRegistryNumber"] = "Grundbuchnummer",
        ["parking"] = "Parkplätze",
        ["features.balcony"] = "Balkon", ["features.terrace"] = "Terrasse", ["features.loggia"] = "Loggia",
        ["features.garden"] = "Garten", ["features.elevator"] = "Lift", ["features.cellar"] = "Keller",
        ["features.atticStorage"] = "Estrich", ["features.storageRoom"] = "Abstellraum",
        ["features.ownWashingMachine"] = "Eigene Waschmaschine", ["features.sharedLaundry"] = "Gemeinsame Waschküche",
        ["features.dishwasher"] = "Geschirrspüler", ["features.fireplace"] = "Cheminée",
        ["features.floorHeating"] = "Bodenheizung", ["features.wheelchairAccessible"] = "Rollstuhlgängig",
        ["features.stepFree"] = "Stufenlos", ["features.petsAllowed"] = "Haustiere erlaubt",
        ["features.smokingAllowed"] = "Rauchen erlaubt", ["features.furnished"] = "Möbliert",
        ["features.view"] = "Aussicht", ["features.orientation"] = "Ausrichtung", ["features.kitchen"] = "Küche",
        ["features.bathroomDetails"] = "Bad", ["features.flooring"] = "Bodenbeläge", ["features.other"] = "Weitere Ausstattung",
        ["energy.heatingType"] = "Heizung", ["energy.heatDistribution"] = "Wärmeverteilung", ["energy.geakClass"] = "GEAK-Klasse",
        ["energy.minergie"] = "Minergie", ["energy.solarPanels"] = "Solaranlage", ["energy.evCharging"] = "Ladestation",
        ["availability.availableFrom"] = "Bezug ab",
        ["viewing.info"] = "Besichtigung", ["viewing.dates"] = "Besichtigungstermine",
        ["application.instructions"] = "Hinweise zur Bewerbung", ["application.requiredDocuments"] = "Benötigte Unterlagen",
        ["application.applicationUrl"] = "Link zur Bewerbung",
        ["contact.company"] = "Firma", ["contact.name"] = "Name", ["contact.role"] = "Funktion",
        ["contact.email"] = "E-Mail", ["contact.phone"] = "Telefon", ["contact.website"] = "Website",
        ["description.summary"] = "Kurzbeschreibung", ["description.original"] = "Originaltext",
        ["documents.hasFloorPlan"] = "Grundriss vorhanden", ["documents.mentionedAttachments"] = "Erwähnte Unterlagen",
        ["otherDetails"] = "Weitere Angaben",
        ["parking.type"] = "Art",
        ["parking.count"] = "Anzahl",
        ["parking.priceChf"] = "Preis CHF",
        ["parking.pricePeriod"] = "Pro",
        ["parking.included"] = "Im Preis inbegriffen",
        ["location.distances.target"] = "Ziel",
        ["location.distances.meters"] = "Meter",
        ["location.distances.minutes"] = "Minuten",
        ["location.distances.mode"] = "Unterwegs",
        ["otherDetails.label"] = "Bezeichnung",
        ["otherDetails.value"] = "Wert"
    };

    private static readonly Dictionary<string, string> Units = new()
    {
        ["size.livingAreaM2"] = SquareMeters, ["size.grossFloorAreaM2"] = SquareMeters, ["size.usableAreaM2"] = SquareMeters,
        ["size.plotAreaM2"] = SquareMeters, ["size.balconyAreaM2"] = SquareMeters, ["size.gardenAreaM2"] = SquareMeters,
        ["size.ceilingHeightM"] = "m",
        ["rent.netChf"] = Francs, ["rent.chargesChf"] = Francs, ["rent.grossChf"] = Francs, ["rent.depositChf"] = Francs,
        ["rent.minimumRentalMonths"] = Months,
        ["sale.priceChf"] = Francs, ["sale.pricePerM2Chf"] = Francs, ["sale.condoFeesChfPerYear"] = Francs,
        ["sale.renovationFundChf"] = Francs
    };

    private static readonly Dictionary<string, string> ChoiceNames = new()
    {
        ["rent"] = "Miete", ["sale"] = "Kauf",
        ["apartment"] = "Wohnung", ["studio"] = "Studio", ["attic"] = "Dachwohnung", ["maisonette"] = "Maisonette",
        ["loft"] = "Loft", ["terrace_apartment"] = "Terrassenwohnung", ["furnished_apartment"] = "Möblierte Wohnung",
        ["room"] = "Zimmer", ["house"] = "Haus", ["parking"] = "Parkplatz", ["commercial"] = "Gewerbe", ["plot"] = "Grundstück",
        ["other"] = "Andere",
        ["new"] = "Neubau", ["first_occupancy"] = "Erstbezug", ["renovated"] = "Renoviert", ["good"] = "Gut",
        ["needs_renovation"] = "Renovationsbedürftig",
        ["akonto"] = "Akonto", ["pauschal"] = "Pauschal", ["unlimited"] = "Unbefristet", ["limited"] = "Befristet",
        ["freehold"] = "Alleineigentum", ["condominium"] = "Stockwerkeigentum", ["co_op"] = "Genossenschaft",
        ["heat_pump"] = "Wärmepumpe", ["district"] = "Fernwärme", ["gas"] = "Gas", ["oil"] = "Öl", ["wood"] = "Holz",
        ["pellets"] = "Pellets", ["electric"] = "Elektro", ["solar"] = "Solar",
        ["floor"] = "Bodenheizung", ["radiators"] = "Radiatoren",
        ["garage"] = "Garage", ["underground"] = "Tiefgarage", ["carport"] = "Carport", ["outdoor"] = "Aussenparkplatz",
        ["month"] = "Monat", ["once"] = "Einmalig",
        ["walk"] = "Zu Fuss", ["transit"] = "ÖV", ["car"] = "Auto", ["bike"] = "Velo"
    };

    public static string Label(string path) => Labels.GetValueOrDefault(path, path.Split('.')[^1]);

    public static string? PathForLabel(string label) =>
        Labels.FirstOrDefault(entry => entry.Value.Equals(label.Trim(), StringComparison.OrdinalIgnoreCase)).Key;

    public static string? Unit(string path) => Units.GetValueOrDefault(path);

    public static string ChoiceName(string value) => ChoiceNames.GetValueOrDefault(value, value);
}
