using System.Text;
using Dialysis.SmartConnect.ExtendedPlugins;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class DestinationSetFilterTransformStageTests
{
    [Fact]
    public async Task Removeallexcept_Narrows_Set_To_Named_Routes_Async()
    {
        var paramsJson = """
        {
          "script": "destinationSet.removeAllExcept(['route-a']);",
          "availableRouteNames": ["route-a", "route-b", "route-c"]
        }
        """;

        var msg = Wrap_Message().WithMetadata(DestinationSetFilterTransformStage.ParametersMetadataKey, paramsJson);

        var result = await new DestinationSetFilterTransformStage().TransformAsync(msg, CancellationToken.None);

        var csv = result.Metadata[DestinationSetFilterTransformStage.DestinationSetMetadataKey];
        Assert.Equal("route-a", csv);
    }

    [Fact]
    public async Task Remove_Drops_Named_Routes_Async()
    {
        var paramsJson = """
        {
          "script": "destinationSet.remove(['route-b']);",
          "availableRouteNames": ["route-a", "route-b", "route-c"]
        }
        """;

        var msg = Wrap_Message().WithMetadata(DestinationSetFilterTransformStage.ParametersMetadataKey, paramsJson);

        var result = await new DestinationSetFilterTransformStage().TransformAsync(msg, CancellationToken.None);

        var csv = result.Metadata[DestinationSetFilterTransformStage.DestinationSetMetadataKey];
        var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Contains("route-a", parts);
        Assert.Contains("route-c", parts);
        Assert.DoesNotContain("route-b", parts);
    }

    [Fact]
    public async Task Removeall_Yields_Empty_Set_Async()
    {
        var paramsJson = """
        {
          "script": "destinationSet.removeAll();",
          "availableRouteNames": ["route-a", "route-b"]
        }
        """;

        var msg = Wrap_Message().WithMetadata(DestinationSetFilterTransformStage.ParametersMetadataKey, paramsJson);

        var result = await new DestinationSetFilterTransformStage().TransformAsync(msg, CancellationToken.None);

        var csv = result.Metadata[DestinationSetFilterTransformStage.DestinationSetMetadataKey];
        Assert.Empty(csv);
    }

    [Fact]
    public async Task No_Parameters_Returns_Message_Unchanged_Async()
    {
        var msg = Wrap_Message();
        var result = await new DestinationSetFilterTransformStage().TransformAsync(msg, CancellationToken.None);
        Assert.False(result.Metadata.ContainsKey(DestinationSetFilterTransformStage.DestinationSetMetadataKey));
    }

    [Fact]
    public async Task Script_Can_Branch_On_Payload_Text_Async()
    {
        var paramsJson = """
        {
          "script": "if (payloadText.indexOf('VIP') !== -1) destinationSet.removeAllExcept(['priority']); else destinationSet.remove(['priority']);",
          "availableRouteNames": ["priority", "default"]
        }
        """;

        var vipMsg = Wrap_Message("VIP-patient").WithMetadata(DestinationSetFilterTransformStage.ParametersMetadataKey, paramsJson);
        var normalMsg = Wrap_Message("regular-patient").WithMetadata(DestinationSetFilterTransformStage.ParametersMetadataKey, paramsJson);

        var stage = new DestinationSetFilterTransformStage();
        var vipResult = await stage.TransformAsync(vipMsg, CancellationToken.None);
        var normalResult = await stage.TransformAsync(normalMsg, CancellationToken.None);

        Assert.Equal("priority", vipResult.Metadata[DestinationSetFilterTransformStage.DestinationSetMetadataKey]);
        Assert.Equal("default", normalResult.Metadata[DestinationSetFilterTransformStage.DestinationSetMetadataKey]);
    }

    private static IntegrationMessage Wrap_Message(string payload = "test") =>
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
