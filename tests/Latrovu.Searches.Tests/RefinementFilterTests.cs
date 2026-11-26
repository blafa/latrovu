namespace Latrovu.Searches.Tests;

public sealed class RefinementFilterTests
{
    private const string Criteria = """{"roomsMin":4,"roomsMax":null,"priceMaxChf":3000,"availableFrom":null,"places":[{"name":"Thun"}]}""";

    [Fact]
    public void Keep_DropsQuestionsAboutGivenCriteria()
    {
        Refinement[] refinements =
        [
            new("rooms", "Wie viele Zimmer sind Ihnen wichtig?"),
            new("budget", "Wie hoch ist Ihr Budget?"),
            new("place", "In welcher Gegend suchen Sie?"),
            new("moveIn", "Wann möchten Sie einziehen?")
        ];

        IReadOnlyList<string> kept = RefinementFilter.Keep(refinements, Criteria);

        Assert.Equal(["Wann möchten Sie einziehen?"], kept);
    }

    [Fact]
    public void Keep_KeepsOpenTopicsAndSkipsEmptyQuestions()
    {
        Refinement[] refinements = [new("features", " Brauchen Sie einen Balkon? "), new("other", " ")];

        IReadOnlyList<string> kept = RefinementFilter.Keep(refinements, Criteria);

        Assert.Equal(["Brauchen Sie einen Balkon?"], kept);
    }
}
