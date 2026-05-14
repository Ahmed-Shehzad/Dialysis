using System.Text;
using Dialysis.SmartConnect.ExtendedPlugins;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class JavascriptRouteFilterTests
{
    [Fact]
    public async Task Truthy_Script_Returns_Allow_Async()
    {
        var filter = new JavascriptRouteFilter();
        var msg = Create_Message("""{"script":"true"}""");

        var result = await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Allow, result.Disposition);
    }

    [Fact]
    public async Task Falsy_Script_Returns_Drop_Async()
    {
        var filter = new JavascriptRouteFilter();
        var msg = Create_Message("""{"script":"false"}""");

        var result = await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Drop, result.Disposition);
    }

    [Fact]
    public async Task Script_Can_Access_Payloadtext_Async()
    {
        var filter = new JavascriptRouteFilter();
        var msg = Create_Message("""{"script":"payloadText.indexOf('hello') >= 0"}""", "hello world");

        var result = await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Allow, result.Disposition);
    }

    [Fact]
    public async Task Script_Can_Access_Metadata_Async()
    {
        var filter = new JavascriptRouteFilter();
        var msg = Create_Message("""{"script":"metadata['x-source'] === 'test'"}""")
            .WithMetadata("x-source", "test");

        var result = await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Allow, result.Disposition);
    }

    [Fact]
    public async Task No_Parameters_Allows_Through_Async()
    {
        var filter = new JavascriptRouteFilter();
        var msg = new IntegrationMessage
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "c1",
            Payload = "data"u8.ToArray(),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Allow, result.Disposition);
    }

    [Fact]
    public async Task Script_Can_Access_Correlationid_And_Flowid_Async()
    {
        var filter = new JavascriptRouteFilter();
        var flowId = Guid.NewGuid();
        var msg = new IntegrationMessage
        {
            Id = Guid.NewGuid(),
            FlowId = flowId,
            CorrelationId = "corr-123",
            Payload = "x"u8.ToArray(),
            PayloadFormat = PayloadFormat.Utf8Text,
            Metadata = System.Collections.Immutable.ImmutableDictionary<string, string>.Empty.Add(
                JavascriptRouteFilter.ParametersMetadataKey,
                """{"script":"correlationId === 'corr-123' && flowId.length > 0"}"""),
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Allow, result.Disposition);
    }

    private static IntegrationMessage Create_Message(string parametersJson, string payload = "test")
    {
        return new IntegrationMessage
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes(payload),
            PayloadFormat = PayloadFormat.Utf8Text,
            Metadata = System.Collections.Immutable.ImmutableDictionary<string, string>.Empty.Add(
                JavascriptRouteFilter.ParametersMetadataKey,
                parametersJson),
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}
