namespace Latrovu.Listings.Review;

public sealed record FieldDefinition(string Path, string Section, FieldKind Kind, IReadOnlyList<string> Choices)
{
    public IReadOnlyList<FieldDefinition> ItemFields { get; init; } = [];

    public string Key => Path.Split('.')[^1];
}
