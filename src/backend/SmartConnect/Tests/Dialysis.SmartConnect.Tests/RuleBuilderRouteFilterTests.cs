using Dialysis.SmartConnect.ExtendedPlugins;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class RuleBuilderRouteFilterTests
{
    private readonly RuleBuilderRouteFilter _Filter = new();

    [Fact]
    public async Task Payloadcontains_All_Match_Allows_Async()
    {
        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            CorrelationId = "c",
            Payload = "hello HL7 world"u8.ToArray(),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        }.WithMetadata(
            "smartconnect.filter.parameters",
            """{"match":"all","rules":[{"type":"payloadContains","value":"HL7"}]}""");

        var r = await _Filter.EvaluateAsync(msg, CancellationToken.None);
        Assert.Equal(RouteFilterDisposition.Allow, r.Disposition);
    }

    [Fact]
    public async Task Payloadcontains_Fails_Drops_Async()
    {
        var msg = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            CorrelationId = "c",
            Payload = "hello"u8.ToArray(),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        }.WithMetadata(
            "smartconnect.filter.parameters",
            """{"match":"all","rules":[{"type":"payloadContains","value":"HL7"}]}""");

        var r = await _Filter.EvaluateAsync(msg, CancellationToken.None);
        Assert.Equal(RouteFilterDisposition.Drop, r.Disposition);
    }
}
