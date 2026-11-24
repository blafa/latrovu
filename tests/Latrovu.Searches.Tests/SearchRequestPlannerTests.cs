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
        Search? created = SearchRequestPlanner.Apply([], Interpretation(SearchAction.New, "Albisrieden"), Address, Channel);

        Assert.NotNull(created);
        Assert.Equal("Albisrieden", created.Name);
        Assert.Equal(Search.ActiveStatus, created.Status);
    }

    [Fact]
    public void New_WithExistingSameName_UpdatesInsteadOfDuplicating()
    {
        Search existing = Existing("Albisrieden");

        Search? result = SearchRequestPlanner.Apply([existing], Interpretation(SearchAction.New, "albisrieden"), Address, Channel);

        Assert.Equal(existing.Id, result?.Id);
    }

    [Fact]
    public void Update_ByName_ChangesThatSearch()
    {
        Search albisrieden = Existing("Albisrieden");
        Search zurich = Existing("Zürich");

        Search? result = SearchRequestPlanner.Apply([albisrieden, zurich], Interpretation(SearchAction.Update, "Zürich"), Address, Channel);

        Assert.Equal(zurich.Id, result?.Id);
    }

    [Fact]
    public void Update_WithoutName_SingleOpenSearch_UpdatesIt()
    {
        Search existing = Existing("Albisrieden");

        Search? result = SearchRequestPlanner.Apply([existing], Interpretation(SearchAction.Update, null), Address, Channel);

        Assert.Equal(existing.Id, result?.Id);
    }

    [Fact]
    public void Stop_UnknownNameWithSeveralSearches_ChangesNothing()
    {
        Search? result = SearchRequestPlanner.Apply(
            [Existing("Albisrieden"), Existing("Zürich")],
            Interpretation(SearchAction.Stop, "Bern"),
            Address,
            Channel);

        Assert.Null(result);
    }

    [Fact]
    public void Pause_ByName_SetsPausedStatus()
    {
        Search? result = SearchRequestPlanner.Apply([Existing("Albisrieden")], Interpretation(SearchAction.Pause, "Albisrieden"), Address, Channel);

        Assert.Equal(Search.PausedStatus, result?.Status);
    }

    private static Search Existing(string name) =>
        new(Guid.NewGuid(), Address, Channel, name, "de", Criteria, Search.ActiveStatus);

    private static SearchInterpretation Interpretation(SearchAction action, string? name) =>
        new("de", action, name, Criteria, "Zusammenfassung", []);
}
