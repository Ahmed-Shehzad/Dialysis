using Dialysis.SmartConnect.ExtendedPlugins;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class RuleBuilderRouteFilterTests
{
    private readonly RuleBuilderRouteFilter _filter = new();

    [Fact]
    public async Task PayloadContains_all_match_allows()
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

        var r = await _filter.EvaluateAsync(msg, CancellationToken.None);
        Assert.Equal(RouteFilterDisposition.Allow, r.Disposition);
    }

    [Fact]
    public async Task PayloadContains_fails_drops()
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

        var r = await _filter.EvaluateAsync(msg, CancellationToken.None);
        Assert.Equal(RouteFilterDisposition.Drop, r.Disposition);
    }
}
