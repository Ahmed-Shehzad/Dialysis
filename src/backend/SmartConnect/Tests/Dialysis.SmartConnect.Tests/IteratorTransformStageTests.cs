using System.Text;
using Dialysis.SmartConnect.ExtendedPlugins;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class IteratorTransformStageTests
{
    [Fact]
    public async Task Iterates_Json_Array_And_Concatenates_Results_Async()
    {
        // Child transform: uppercase the element.
        var paramsJson = """
        {
          "iterableExpression": "$.observations[*]",
          "child": {
            "kind": "javascript",
            "parametersJson": "{\"script\":\"payloadText.toUpperCase()\"}"
          },
          "separator": "|"
        }
        """;

        var msg = Wrap_Json("""{"observations":["a","b","c"]}""");
        msg = msg.WithMetadata(IteratorTransformStage.ParametersMetadataKey, paramsJson);

        var result = await Build_Stage().TransformAsync(msg, CancellationToken.None);

        var text = Encoding.UTF8.GetString(result.Payload.Span);
        // Child operates on each element's JSON form ("a"), so result is each uppercased and joined by "|".
        Assert.Contains("\"A\"", text);
        Assert.Contains("\"B\"", text);
        Assert.Contains("\"C\"", text);
        Assert.Equal(3, text.Split('|', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public async Task Iterates_Hl7_Segments_And_Replaces_Payload_Async()
    {
        var paramsJson = """
        {
          "iterableExpression": "OBX",
          "child": {
            "kind": "javascript",
            "parametersJson": "{\"script\":\"payloadText.toUpperCase()\"}"
          },
          "separator": "\n"
        }
        """;

        var msg = Wrap_Hl7(
            "MSH|^~\\&|SRC|FAC|DEST|FAC|202601010000||ORU^R01|1|P|2.5\r" +
            "OBX|1|NM||first\r" +
            "OBX|2|NM||second");
        msg = msg.WithMetadata(IteratorTransformStage.ParametersMetadataKey, paramsJson);

        var result = await Build_Stage().TransformAsync(msg, CancellationToken.None);
        var text = Encoding.UTF8.GetString(result.Payload.Span);

        Assert.Contains("FIRST", text);
        Assert.Contains("SECOND", text);
    }

    [Fact]
    public async Task No_Parameters_Returns_Message_Unchanged_Async()
    {
        var msg = Wrap_Json("""{"observations":["a"]}""");

        var result = await Build_Stage().TransformAsync(msg, CancellationToken.None);

        Assert.Equal(msg.Payload, result.Payload);
    }

    [Fact]
    public async Task Empty_Iterable_Returns_Message_Unchanged_Async()
    {
        var paramsJson = """
        {
          "iterableExpression": "$.missing[*]",
          "child": { "kind": "javascript", "parametersJson": "{\"script\":\"payloadText\"}" }
        }
        """;
        var msg = Wrap_Json("""{"observations":["a"]}""");
        msg = msg.WithMetadata(IteratorTransformStage.ParametersMetadataKey, paramsJson);

        var result = await Build_Stage().TransformAsync(msg, CancellationToken.None);

        Assert.Equal(msg.Payload, result.Payload);
    }

    private static IteratorTransformStage Build_Stage()
    {
        var services = new ServiceCollection();
        var registry = new MutableFlowPluginRegistry();
        registry.RegisterTransformStage(new JavascriptTransformStage());
        services.AddSingleton<IFlowPluginRegistry>(registry);
        var sp = services.BuildServiceProvider();
        return new IteratorTransformStage(sp);
    }

    private static IntegrationMessage Wrap_Json(string payload) =>
        new()
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes(payload),
            PayloadFormat = PayloadFormat.Json,
            Metadata = [],
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

    private static IntegrationMessage Wrap_Hl7(string payload) =>
        new()
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes(payload),
            PayloadFormat = PayloadFormat.Utf8Text,
            Metadata = [],
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
}
