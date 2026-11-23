using System.Text.Json;

namespace Latrovu.BuildingBlocks.Infrastructure.Messaging;

public static class OutboxSerializer
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    public static (string TypeName, string Payload) Serialize(object message)
    {
        ArgumentNullException.ThrowIfNull(message);

        Type type = message.GetType();
        string typeName = type.AssemblyQualifiedName ?? throw new InvalidOperationException($"Type {type} has no name");
        return (typeName, JsonSerializer.Serialize(message, type, WebJson));
    }

    public static object Deserialize(string typeName, string payload)
    {
        Type type = Type.GetType(typeName, throwOnError: true)!;
        return JsonSerializer.Deserialize(payload, type, WebJson)
            ?? throw new InvalidOperationException($"Outbox payload for {type.Name} is empty");
    }
}
