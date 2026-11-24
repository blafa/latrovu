using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Latrovu.Listings.Infrastructure.Viewing.Chat;

public sealed partial class ContactRedactor
{
    private const int MinimumTermLength = 3;
    private const int MinimumCompanyWordLength = 4;
    private static readonly string[] ContactKeys = ["contact", "agent", "agency", "provider", "company"];

    private readonly IReadOnlyList<string> terms;

    private ContactRedactor(IReadOnlyList<string> terms)
    {
        this.terms = terms;
    }

    public static ContactRedactor FromListingData(string listingDataJson)
    {
        List<string> found = [];
        try
        {
            Collect(JsonNode.Parse(listingDataJson), insideContact: false, companyKey: false, found);
        }
        catch (System.Text.Json.JsonException)
        {
            return new ContactRedactor([]);
        }

        return new ContactRedactor(found.Distinct(StringComparer.OrdinalIgnoreCase).OrderByDescending(term => term.Length).ToList());
    }

    public string Redact(string text, string providerWord)
    {
        string result = text;
        foreach (string term in terms)
        {
            result = Regex.Replace(result, $@"\b{Regex.Escape(term)}\b", providerWord, RegexOptions.IgnoreCase);
        }

        result = EmailPattern().Replace(result, providerWord);
        return PhonePattern().Replace(result, string.Empty).Trim();
    }

    private static void Collect(JsonNode? node, bool insideContact, bool companyKey, List<string> found)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach ((string key, JsonNode? child) in obj)
                {
                    bool contactKey = ContactKeys.Any(name => key.Contains(name, StringComparison.OrdinalIgnoreCase));
                    Collect(child, insideContact || contactKey, key.Contains("company", StringComparison.OrdinalIgnoreCase) || key.Contains("agency", StringComparison.OrdinalIgnoreCase), found);
                }

                break;
            case JsonArray array:
                foreach (JsonNode? item in array)
                {
                    Collect(item, insideContact, companyKey, found);
                }

                break;
            case JsonValue value when insideContact && value.TryGetValue(out string? text) && text.Trim().Length >= MinimumTermLength:
                found.Add(text.Trim());
                AddCompanyWord(text.Trim(), companyKey, found);
                break;
        }
    }

    private static void AddCompanyWord(string text, bool companyKey, List<string> found)
    {
        string firstWord = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        if (companyKey && firstWord.Length >= MinimumCompanyWordLength)
        {
            found.Add(firstWord);
        }
    }

    [GeneratedRegex(@"[\w.+-]+@[\w-]+(\.[\w-]+)+")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"(\+41|0041|\b0)\s?\d{2}[\s/.-]?\d{3}[\s.-]?\d{2}[\s.-]?\d{2}\b")]
    private static partial Regex PhonePattern();
}
