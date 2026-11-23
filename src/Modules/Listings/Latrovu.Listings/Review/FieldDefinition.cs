namespace Latrovu.Listings.Review;

public sealed record FieldDefinition(string Path, string Section, FieldKind Kind, IReadOnlyList<string> Choices);
