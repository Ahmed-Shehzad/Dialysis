using System.Collections.Immutable;
using System.Text;
using Dialysis.SmartConnect.Transforms;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Transforms;

/// <summary>
/// Covers the two verify-hl7 plugins: the route filter (drop-on-fail) + the strict transform stage
/// (throw-on-fail). Both share <c>VerifyHl7Core.Inspect</c>; tests assert both surface the same
/// verdict per scenario.
/// </summary>
public sealed class VerifyHl7Tests
{
    private const string ValidMessage =
        "MSH|^~\\&|HIS|HOSPITAL|EMR|CLINIC|20260526120000||ADT^A01|MSG-1|P|2.5\r" +
        "PID|||MRN-12345||DOE^JOHN\r" +
        "PV1|1|I|ICU^101";

    [Fact]
    public async Task Filter_Allows_Well_Formed_Hl7_Message_Async()
    {
        var filter = new VerifyHl7RouteFilter();
        var result = await filter.EvaluateAsync(NewMessage(ValidMessage, null), CancellationToken.None);
        Assert.Equal(RouteFilterDisposition.Allow, result.Disposition);
    }

    [Fact]
    public async Task Filter_Drops_Non_Hl7_Payload_Async()
    {
        var filter = new VerifyHl7RouteFilter();
        var result = await filter.EvaluateAsync(NewMessage("not an hl7 message", null), CancellationToken.None);
        Assert.Equal(RouteFilterDisposition.Drop, result.Disposition);
    }

    [Fact]
    public async Task Filter_Drops_When_Required_Segment_Missing_Async()
    {
        var filter = new VerifyHl7RouteFilter();
        var minimalAdt = "MSH|^~\\&|HIS|HOSPITAL|EMR|CLINIC|20260526120000||ADT^A01|MSG-2|P|2.5\rPID|||MRN-9";
        var result = await filter.EvaluateAsync(
            NewMessage(minimalAdt, """{"requiredSegments":["MSH","PID","PV1"]}""", isFilter: true),
            CancellationToken.None);
        Assert.Equal(RouteFilterDisposition.Drop, result.Disposition);
    }

    [Fact]
    public async Task Filter_Drops_When_Version_Below_Min_Async()
    {
        var filter = new VerifyHl7RouteFilter();
        var oldVersion =
            "MSH|^~\\&|HIS|HOSPITAL|EMR|CLINIC|20260526120000||ADT^A01|MSG-3|P|2.3\r" +
            "PID|||MRN-9";
        var result = await filter.EvaluateAsync(
            NewMessage(oldVersion, """{"minVersion":"2.5"}""", isFilter: true),
            CancellationToken.None);
        Assert.Equal(RouteFilterDisposition.Drop, result.Disposition);
    }

    [Fact]
    public async Task Strict_Stage_Throws_On_Bad_Hl7_Async()
    {
        var stage = new VerifyHl7TransformStage();
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await stage.TransformAsync(NewMessage("nope", null), CancellationToken.None));
    }

    [Fact]
    public async Task Strict_Stage_Passes_Through_Valid_Hl7_Unchanged_Async()
    {
        var stage = new VerifyHl7TransformStage();
        var input = NewMessage(ValidMessage, null);
        var output = await stage.TransformAsync(input, CancellationToken.None);
        Assert.Equal(input.Payload.ToArray(), output.Payload.ToArray());
    }

    private static IntegrationMessage NewMessage(string payload, string? parametersJson, bool isFilter = false)
    {
        var metaKey = isFilter ? "smartconnect.filter.parameters" : "smartconnect.transform.parameters";
        var meta = ImmutableDictionary<string, string>.Empty;
        if (parametersJson is not null)
        {
            meta = meta.Add(metaKey, parametersJson);
        }
        return new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            CorrelationId = "c-" + Guid.NewGuid().ToString("N")[..8],
            Payload = Encoding.UTF8.GetBytes(payload),
            PayloadFormat = PayloadFormat.Utf8Text,
            Metadata = meta,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}
