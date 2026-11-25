using System.Globalization;
using System.Text.RegularExpressions;

namespace Latrovu.Listings.Viewing.Viewings;

public static partial class SlotChoice
{
    private const int MaxChoiceLength = 120;

    private static readonly Dictionary<string, DayOfWeek> Weekdays = new(StringComparer.Ordinal)
    {
        ["montag"] = DayOfWeek.Monday, ["dienstag"] = DayOfWeek.Tuesday, ["mittwoch"] = DayOfWeek.Wednesday,
        ["donnerstag"] = DayOfWeek.Thursday, ["freitag"] = DayOfWeek.Friday, ["samstag"] = DayOfWeek.Saturday, ["sonntag"] = DayOfWeek.Sunday,
        ["lundi"] = DayOfWeek.Monday, ["mardi"] = DayOfWeek.Tuesday, ["mercredi"] = DayOfWeek.Wednesday, ["jeudi"] = DayOfWeek.Thursday,
        ["vendredi"] = DayOfWeek.Friday, ["samedi"] = DayOfWeek.Saturday, ["dimanche"] = DayOfWeek.Sunday,
        ["lunedì"] = DayOfWeek.Monday, ["martedì"] = DayOfWeek.Tuesday, ["mercoledì"] = DayOfWeek.Wednesday, ["giovedì"] = DayOfWeek.Thursday,
        ["venerdì"] = DayOfWeek.Friday, ["sabato"] = DayOfWeek.Saturday, ["domenica"] = DayOfWeek.Sunday,
        ["monday"] = DayOfWeek.Monday, ["tuesday"] = DayOfWeek.Tuesday, ["wednesday"] = DayOfWeek.Wednesday, ["thursday"] = DayOfWeek.Thursday,
        ["friday"] = DayOfWeek.Friday, ["saturday"] = DayOfWeek.Saturday, ["sunday"] = DayOfWeek.Sunday
    };

    private static readonly Dictionary<string, int> Ordinals = new(StringComparer.Ordinal)
    {
        ["erste"] = 1, ["ersten"] = 1, ["zweite"] = 2, ["zweiten"] = 2, ["dritte"] = 3, ["dritten"] = 3,
        ["premier"] = 1, ["première"] = 1, ["deuxième"] = 2, ["troisième"] = 3,
        ["primo"] = 1, ["prima"] = 1, ["secondo"] = 2, ["seconda"] = 2, ["terzo"] = 3, ["terza"] = 3,
        ["first"] = 1, ["third"] = 3
    };

    public static ViewingSlot? Match(string text, IReadOnlyList<ViewingSlot> offered)
    {
        ArgumentNullException.ThrowIfNull(offered);

        if (string.IsNullOrWhiteSpace(text) || text.Length > MaxChoiceLength || offered.Count == 0)
        {
            return null;
        }

        string[] words = WordPattern().Matches(text.ToLowerInvariant()).Select(match => match.Value).ToArray();
        List<IReadOnlyCollection<ViewingSlot>> clues = new[] { ByPosition(words, offered), ByWeekday(words, offered), ByDayOrHour(words, offered) }
            .OfType<IReadOnlyCollection<ViewingSlot>>()
            .ToList();
        if (clues.Count == 0)
        {
            return null;
        }

        List<ViewingSlot> remaining = clues.Aggregate((IEnumerable<ViewingSlot>)offered, (current, clue) => current.Intersect(clue)).ToList();
        return remaining.Count == 1 ? remaining[0] : null;
    }

    private static IReadOnlyCollection<ViewingSlot>? ByPosition(string[] words, IReadOnlyList<ViewingSlot> offered)
    {
        int? position = words.Length == 1 && int.TryParse(words[0], NumberStyles.None, CultureInfo.InvariantCulture, out int number)
            ? number
            : words.Select(word => Ordinals.TryGetValue(word, out int ordinal) ? ordinal : (int?)null).FirstOrDefault(value => value is not null);
        return position is >= 1 && position <= offered.Count ? [offered[position.Value - 1]] : null;
    }

    private static IReadOnlyCollection<ViewingSlot>? ByWeekday(string[] words, IReadOnlyList<ViewingSlot> offered)
    {
        HashSet<DayOfWeek> days = words.Where(Weekdays.ContainsKey).Select(word => Weekdays[word]).ToHashSet();
        return days.Count == 0 ? null : offered.Where(slot => days.Contains(SwissTime.ToLocal(slot.StartsAt).DayOfWeek)).ToList();
    }

    private static IReadOnlyCollection<ViewingSlot>? ByDayOrHour(string[] words, IReadOnlyList<ViewingSlot> offered)
    {
        if (words.Length == 1)
        {
            return null;
        }

        HashSet<int> numbers = words.Select(word => int.TryParse(word, NumberStyles.None, CultureInfo.InvariantCulture, out int value) ? value : -1)
            .Where(value => value >= 0)
            .ToHashSet();
        List<ViewingSlot> hits = offered.Where(slot =>
            numbers.Contains(SwissTime.ToLocal(slot.StartsAt).Day) || numbers.Contains(SwissTime.ToLocal(slot.StartsAt).Hour)).ToList();
        return hits.Count > 0 ? hits : null;
    }

    [GeneratedRegex(@"[\p{L}]+|\d+")]
    private static partial Regex WordPattern();
}
