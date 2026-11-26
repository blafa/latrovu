namespace Latrovu.Listings.Infrastructure.Review.Web;

internal sealed record IntakeListingSummary(
    string Title,
    IReadOnlyList<SourceCount> SourceCounts,
    IReadOnlyList<string> OpenLabels)
{
    private const string TitlePath = "title";

    public static IntakeListingSummary From(ListingReview review)
    {
        ArgumentNullException.ThrowIfNull(review);

        ReviewModel model = ReviewModel.From(string.Empty, review);
        FieldDefinition? titleField = ListingFieldCatalog.Fields.FirstOrDefault(field => field.Path == TitlePath);
        string title = titleField is null ? string.Empty : ReviewFieldRenderer.DisplayText(titleField, model.ValueOf(titleField));
        List<SourceCount> sourceCounts = model.Filled
            .GroupBy(model.SourceOf)
            .OrderBy(group => group.Key)
            .Select(group => new SourceCount(group.Key, group.Count()))
            .ToList();
        List<string> openLabels = model.Open.Select(field => FieldLabels.Label(field.Path)).ToList();
        return new IntakeListingSummary(title, sourceCounts, openLabels);
    }
}
