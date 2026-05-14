using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class IntegrationMessageTests
{
    [Fact]
    public void With_Metadata_Preserves_Payload_And_Merges_Keys()
    {
        var id = Guid.NewGuid();
        var flowId = Guid.NewGuid();
        var original = new IntegrationMessage
        {
            Id = id,
            FlowId = flowId,
            CorrelationId = "c1",
            Payload = "body"u8.ToArray(),
            PayloadFormat = PayloadFormat.Binary,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var m1 = original.WithMetadata("k1", "a");
        var m2 = m1.WithMetadata("k1", "b").WithMetadata("k2", "c");

        Assert.Same(original.Metadata, System.Collections.Immutable.ImmutableDictionary<string, string>.Empty);
        Assert.Equal("a", m1.Metadata["k1"]);
        Assert.Equal("b", m2.Metadata["k1"]);
        Assert.Equal("c", m2.Metadata["k2"]);
        Assert.Equal(original.Payload.ToArray(), m2.Payload.ToArray());
        Assert.Equal(id, m2.Id);
    }
}
