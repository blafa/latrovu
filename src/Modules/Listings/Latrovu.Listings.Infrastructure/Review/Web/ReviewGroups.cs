namespace Latrovu.Listings.Infrastructure.Review.Web;

internal static class ReviewGroups
{
    public static readonly IReadOnlyList<ReviewGroup> All =
    [
        new("Objekt", ReviewIcons.Home, [ListingFieldCatalog.GeneralSection, "size", "building"]),
        new("Preis", ReviewIcons.Tag, ["rent", "sale", "parking"]),
        new("Lage", ReviewIcons.MapPin, ["location"]),
        new("Ausstattung", ReviewIcons.Sparkles, ["features", "energy"]),
        new("Kontakt und Termine", ReviewIcons.User, ["contact", "availability", "viewing", "application"]),
        new("Beschreibung", ReviewIcons.Text, ["description", "documents", "otherDetails"])
    ];

    public static string LabelOf(FieldDefinition field) =>
        All.FirstOrDefault(group => group.Sections.Contains(field.Section))?.Label ?? string.Empty;
}
