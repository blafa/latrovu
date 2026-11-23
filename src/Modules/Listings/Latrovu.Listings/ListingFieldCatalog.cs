using System.Text.Json;
using System.Text.Json.Nodes;

namespace Latrovu.Listings;

public static class ListingFieldCatalog
{
    public const string GeneralSection = "general";
    private const string EvidenceProperty = "evidence";
    private const string PropertiesKey = "properties";
    private const string TypeKey = "type";
    private const string EnumKey = "enum";
    private const string ItemsKey = "items";
    private const string NullType = "null";
    private const string ObjectType = "object";
    private const string NumberType = "number";
    private const string BooleanType = "boolean";
    private const string ArrayType = "array";
    private const string StringType = "string";

    public static readonly IReadOnlyList<FieldDefinition> Fields = Build(ListingSchema.Value.Schema);

    public static FieldDefinition? Find(string path) => Fields.FirstOrDefault(field => field.Path == path);

    private static List<FieldDefinition> Build(string schemaJson)
    {
        JsonObject properties = JsonNode.Parse(schemaJson)![PropertiesKey]!.AsObject();
        List<FieldDefinition> fields = [];
        foreach (KeyValuePair<string, JsonNode?> property in properties.Where(property => property.Key != EvidenceProperty))
        {
            bool isGroup = MainType(property.Value!.AsObject()) == ObjectType;
            Collect(property.Key, isGroup ? property.Key : GeneralSection, property.Value!.AsObject(), fields);
        }

        return fields;
    }

    private static void Collect(string path, string section, JsonObject schema, List<FieldDefinition> fields)
    {
        if (MainType(schema) == ObjectType && schema[PropertiesKey] is JsonObject children)
        {
            foreach (KeyValuePair<string, JsonNode?> child in children)
            {
                Collect($"{path}.{child.Key}", section, child.Value!.AsObject(), fields);
            }

            return;
        }

        fields.Add(new FieldDefinition(path, section, KindOf(schema), ChoicesOf(schema)));
    }

    private static FieldKind KindOf(JsonObject schema)
    {
        if (schema[EnumKey] is not null)
        {
            return FieldKind.Choice;
        }

        return MainType(schema) switch
        {
            NumberType => FieldKind.Number,
            BooleanType => FieldKind.Boolean,
            ArrayType => schema[ItemsKey]?[TypeKey]?.GetValue<string>() == StringType ? FieldKind.TextList : FieldKind.Json,
            _ => FieldKind.Text
        };
    }

    private static List<string> ChoicesOf(JsonObject schema) =>
        schema[EnumKey] is JsonArray values
            ? values.Where(value => value is not null).Select(value => value!.GetValue<string>()).ToList()
            : [];

    private static string? MainType(JsonObject schema) => schema[TypeKey] switch
    {
        JsonArray types => types.Select(type => type?.GetValue<string>()).FirstOrDefault(type => type != NullType),
        JsonValue type when type.GetValueKind() == JsonValueKind.String => type.GetValue<string>(),
        _ => null
    };
}
