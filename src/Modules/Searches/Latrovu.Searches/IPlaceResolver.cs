namespace Latrovu.Searches;

public interface IPlaceResolver
{
    Task<PlaceResolution> ResolveAsync(IReadOnlyList<PlaceExpression> places, CancellationToken ct);
}
