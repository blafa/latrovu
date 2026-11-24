namespace Latrovu.Searches;

public sealed record SearchInterpretation(
    string Language,
    SearchAction Action,
    string? SearchName,
    string CriteriaJson,
    string Summary,
    IReadOnlyList<string> Refinements,
    IReadOnlyList<PlaceExpression> Places);
