namespace Latrovu.Searches.Infrastructure;

internal static class SearchCriteria
{
    private const string PriceTolerance = "1.05";

    public static string Condition(string offerType, string rooms, string price, string livingArea) => $"""
        (s.criteria->>'offerType' IS NULL OR {offerType} IS NULL OR s.criteria->>'offerType' = {offerType})
          AND ({rooms} IS NULL OR s.criteria->>'roomsMin' IS NULL OR {rooms} >= (s.criteria->>'roomsMin')::numeric)
          AND ({rooms} IS NULL OR s.criteria->>'roomsMax' IS NULL OR {rooms} <= (s.criteria->>'roomsMax')::numeric)
          AND ({price} IS NULL OR s.criteria->>'priceMaxChf' IS NULL OR {price} <= (s.criteria->>'priceMaxChf')::numeric * {PriceTolerance})
          AND ({livingArea} IS NULL OR s.criteria->>'livingAreaMinM2' IS NULL OR {livingArea} >= (s.criteria->>'livingAreaMinM2')::numeric)
        """;
}
