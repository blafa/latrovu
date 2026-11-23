using Latrovu.BuildingBlocks.Infrastructure.Messaging;
using Xunit;

namespace Latrovu.BuildingBlocks.Tests;

public sealed class OutboxSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_RoundTripsRecord()
    {
        SampleMessage original = new(Guid.NewGuid(), "Kandergrund", 2.5m);

        (string typeName, string payload) = OutboxSerializer.Serialize(original);
        object restored = OutboxSerializer.Deserialize(typeName, payload);

        Assert.Equal(original, restored);
    }

    [Fact]
    public void Serialize_UsesCamelCaseJson()
    {
        (_, string payload) = OutboxSerializer.Serialize(new SampleMessage(Guid.Empty, "Bern", 3m));

        Assert.Contains("\"city\":\"Bern\"", payload);
    }

    public sealed record SampleMessage(Guid ListingId, string City, decimal Rooms);
}
