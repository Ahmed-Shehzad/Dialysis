using System.Collections.Immutable;
using System.Text;
using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.ExtendedPlugins;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class IteratorRouteFilterTests
{
    [Fact]
    public async Task Allows_when_any_OBX_segment_matches()
    {
        // Child filter: javascript returning true when payloadText contains "MATCH".
        var paramsJson = """
        {
          "iterableExpression": "OBX",
          "child": {
            "kind": "javascript",
            "parametersJson": "{\"script\":\"payloadText.indexOf('MATCH') !== -1\"}"
          },
          "minMatches": 1
        }
        """;

        var msg = WrapHl7(
            "MSH|^~\\&|SRC|FAC|DEST|FAC|202601010000||ORU^R01|1|P|2.5\r" +
            "OBX|1|NM|1234^^MDC||no-match-here||mm[Hg]\r" +
            "OBX|2|NM|1235^^MDC||MATCH-here||mm[Hg]");
        msg = msg.WithMetadata(IteratorRouteFilter.ParametersMetadataKey, paramsJson);

        var result = await BuildFilter().EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Allow, result.Disposition);
    }

    [Fact]
    public async Task Drops_when_no_segments_match()
    {
        var paramsJson = """
        {
          "iterableExpression": "OBX",
          "child": {
            "kind": "javascript",
            "parametersJson": "{\"script\":\"payloadText.indexOf('MATCH') !== -1\"}"
          },
          "minMatches": 1
        }
        """;

        var msg = WrapHl7(
            "MSH|^~\\&|SRC|FAC|DEST|FAC|202601010000||ORU^R01|1|P|2.5\r" +
            "OBX|1|NM|1234^^MDC||value-a\r" +
            "OBX|2|NM|1235^^MDC||value-b");
        msg = msg.WithMetadata(IteratorRouteFilter.ParametersMetadataKey, paramsJson);

        var result = await BuildFilter().EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Drop, result.Disposition);
    }

    [Fact]
    public async Task MinMatches_requires_threshold_matches()
    {
        // 2 matches present, threshold 3 → drop.
        var paramsJson = """
        {
          "iterableExpression": "OBX",
          "child": {
            "kind": "javascript",
            "parametersJson": "{\"script\":\"payloadText.indexOf('MATCH') !== -1\"}"
          },
          "minMatches": 3
        }
        """;

        var msg = WrapHl7(
            "MSH|^~\\&|SRC|FAC|DEST|FAC|202601010000||ORU^R01|1|P|2.5\r" +
            "OBX|1|NM||MATCH-a\r" +
            "OBX|2|NM||MATCH-b\r" +
            "OBX|3|NM||no-match");
        msg = msg.WithMetadata(IteratorRouteFilter.ParametersMetadataKey, paramsJson);

        var result = await BuildFilter().EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Drop, result.Disposition);
    }

    [Fact]
    public async Task Empty_iterable_drops()
    {
        var paramsJson = """
        {
          "iterableExpression": "ZZZ",
          "child": { "kind": "javascript", "parametersJson": "{\"script\":\"true\"}" }
        }
        """;

        var msg = WrapHl7(
            "MSH|^~\\&|SRC|FAC|DEST|FAC|202601010000||ORU^R01|1|P|2.5\r" +
            "PID|||MRN-1");
        msg = msg.WithMetadata(IteratorRouteFilter.ParametersMetadataKey, paramsJson);

        var result = await BuildFilter().EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(RouteFilterDisposition.Drop, result.Disposition);
    }

    private static IteratorRouteFilter BuildFilter()
    {
        var services = new ServiceCollection();
        var registry = new MutableFlowPluginRegistry();
        registry.RegisterRouteFilter(new AllowAllRouteFilter());
        registry.RegisterRouteFilter(new JavascriptRouteFilter());
        services.AddSingleton<IFlowPluginRegistry>(registry);
        var sp = services.BuildServiceProvider();
        return new IteratorRouteFilter(sp);
    }

    private static IntegrationMessage WrapHl7(string payload) =>
        new()
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes(payload),
            PayloadFormat = PayloadFormat.Utf8Text,
            Metadata = ImmutableDictionary<string, string>.Empty,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
}
