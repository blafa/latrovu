using System.Text.Json.Nodes;

namespace Latrovu.Listings.Infrastructure.Review.Web;

internal static class ListingValues
{
    public const string NoScore = "none";
    private const string AddressSeparator = ", ";

    public static string? Text(JsonObject data, string path) =>
        JsonPaths.Get(data, path) is JsonValue value && value.TryGetValue(out string? text) && !string.IsNullOrWhiteSpace(text)
            ? text
            : null;

    public static string Address(JsonObject data) =>
        string.Join(AddressSeparator, new[]
        {
            Join(Text(data, "location.street"), Text(data, "location.houseNumber")),
            Join(Text(data, "location.zip"), Text(data, "location.city"))
        }.Where(part => part.Length > 0));

    public static string ScoreOf(JsonObject scores, string path) =>
        scores[path] is JsonValue score && score.TryGetValue(out string? name) ? name : NoScore;

    private static string Join(string? first, string? second) =>
        string.Join(' ', new[] { first, second }.Where(part => !string.IsNullOrWhiteSpace(part)));
}
