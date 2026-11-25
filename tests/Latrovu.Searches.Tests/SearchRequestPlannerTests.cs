using Latrovu.Searches;
using Xunit;

namespace Latrovu.Searches.Tests;

public sealed class SearchRequestPlannerTests
{
    private const string Address = "seeker@example.com";
    private const string Channel = "email";
    private const string Criteria = "{}";

    [Fact]
    public void New_WithoutExisting_CreatesActiveSearchNamedAfterPlace()
    {
        Search? created = Single([], Interpretation(SearchAction.New, "Albisrieden"));

        Assert.NotNull(created);
        Assert.Equal("Albisrieden", created.Name);
        Assert.Equal(Search.ActiveStatus, created.Status);
    }

    [Fact]
    public void New_WithExistingSameName_UpdatesInsteadOfDuplicating()
    {
        Search existing = Existing("Albisrieden");

        Search? result = Single([existing], Interpretation(SearchAction.New, "albisrieden"));

        Assert.Equal(existing.Id, result?.Id);
    }

    [Fact]
    public void Update_ByName_ChangesThatSearch()
    {
        Search albisrieden = Existing("Albisrieden");
        Search zurich = Existing("Zürich");

        Search? result = Single([albisrieden, zurich], Interpretation(SearchAction.Update, "Zürich"));

        Assert.Equal(zurich.Id, result?.Id);
    }

    [Fact]
    public void Update_WithoutName_SingleOpenSearch_UpdatesIt()
    {
        Search existing = Existing("Albisrieden");

        Search? result = Single([existing], Interpretation(SearchAction.Update, null));

        Assert.Equal(existing.Id, result?.Id);
    }

    [Fact]
    public void Update_WithUnknownName_CreatesNewSearchInsteadOfOverwriting()
    {
        Search existing = Existing("Wiedikon oder Kreis 4");

        Search? result = Single([existing], Interpretation(SearchAction.Update, "Thun"));

        Assert.NotEqual(existing.Id, result?.Id);
        Assert.Equal("Thun", result?.Name);
    }

    [Fact]
    public void Stop_UnknownNameWithSeveralSearches_ChangesNothing()
    {
        IReadOnlyList<Search> result = SearchRequestPlanner.Apply(
            [Existing("Albisrieden"), Existing("Zürich")],
            Interpretation(SearchAction.Stop, "Bern"),
            Address,
            Channel);

        Assert.Empty(result);
    }

    [Fact]
    public void Stop_AppliesToAll_StopsEveryOpenSearch()
    {
        IReadOnlyList<Search> result = SearchRequestPlanner.Apply(
            [Existing("Albisrieden"), Existing("Zürich")],
            Interpretation(SearchAction.Stop, null) with { AppliesToAll = true },
            Address,
            Channel);

        Assert.Equal(2, result.Count);
        Assert.All(result, search => Assert.Equal(Search.StoppedStatus, search.Status));
    }

    [Fact]
    public void Pause_ByName_SetsPausedStatus()
    {
        Search? result = Single([Existing("Albisrieden")], Interpretation(SearchAction.Pause, "Albisrieden"));

        Assert.Equal(Search.PausedStatus, result?.Status);
    }

    [Fact]
    public void Update_WithoutName_ReplyToSearch_UpdatesThatSearch()
    {
        Search albisrieden = Existing("Albisrieden");
        Search zurich = Existing("Zürich");

        Search? result = SearchRequestPlanner.Apply(
            [albisrieden, zurich],
            Interpretation(SearchAction.Update, null),
            Address,
            Channel,
            albisrieden.Id).SingleOrDefault();

        Assert.Equal(albisrieden.Id, result?.Id);
    }

    [Fact]
    public void Update_WithName_ReplyToOtherSearch_NameWins()
    {
        Search albisrieden = Existing("Albisrieden");
        Search zurich = Existing("Zürich");

        Search? result = SearchRequestPlanner.Apply(
            [albisrieden, zurich],
            Interpretation(SearchAction.Update, "Zürich"),
            Address,
            Channel,
            albisrieden.Id).SingleOrDefault();

        Assert.Equal(zurich.Id, result?.Id);
    }

    [Fact]
    public void Stop_WithoutName_ReplyToSearch_StopsThatSearch()
    {
        Search albisrieden = Existing("Albisrieden");

        Search? result = SearchRequestPlanner.Apply(
            [albisrieden, Existing("Zürich")],
            Interpretation(SearchAction.Stop, null),
            Address,
            Channel,
            albisrieden.Id).SingleOrDefault();

        Assert.Equal(albisrieden.Id, result?.Id);
        Assert.Equal(Search.StoppedStatus, result?.Status);
    }

    private static Search? Single(IReadOnlyList<Search> existing, SearchInterpretation interpretation) =>
        SearchRequestPlanner.Apply(existing, interpretation, Address, Channel).SingleOrDefault();

    private static Search Existing(string name) =>
        new(Guid.NewGuid(), Address, Channel, name, "de", Criteria, Search.ActiveStatus);

    private static SearchInterpretation Interpretation(SearchAction action, string? name) =>
        new("de", action, name, Criteria, "Zusammenfassung", [], []);
}
