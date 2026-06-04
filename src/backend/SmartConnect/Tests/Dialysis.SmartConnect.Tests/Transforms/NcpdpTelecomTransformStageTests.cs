using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.DataTypes.Ncpdp;
using Dialysis.SmartConnect.ExtendedPlugins;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Transforms;

/// <summary>
/// Slice K: NCPDP Telecom payloads parse into a JSON-addressable shape so downstream
/// JSONPath / mapper / JavaScript transforms can route pharmacy claims by transaction
/// code / NDC / patient without reading control characters by hand.
/// </summary>
public sealed class NcpdpTelecomTransformStageTests
{
    private const char Fs = NcpdpTelecomMessage.FieldSeparator;
    private const char Ss = NcpdpTelecomMessage.SegmentSeparator;

    [Fact]
    public async Task Transform_Projects_Transaction_Header_Fields_Async()
    {
        var payload = BuildPayload(
            // Transaction header: BIN A1=610515, Version A2=D0, Transaction Code A3=B1
            $"A1610515{Fs}A2D0{Fs}A3B1{Fs}A4PCN001",
            // Patient segment: AM=01, Cardholder ID C2=PT-123
            $"AM01{Fs}C2PT-123");
        var stage = new NcpdpTelecomTransformStage();

        var transformed = await stage.TransformAsync(BuildMessage(payload), CancellationToken.None);

        var json = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span)).RootElement;
        Assert.Equal("B1", json.GetProperty("transactionCode").GetString());
        Assert.Equal("D0", json.GetProperty("versionRelease").GetString());

        var segments = json.GetProperty("segments");
        Assert.Equal(2, segments.GetArrayLength());
        Assert.Equal("610515", segments[0].GetProperty("fields").GetProperty("A1").GetString());
        Assert.Equal("PCN001", segments[0].GetProperty("fields").GetProperty("A4").GetString());
    }

    [Fact]
    public async Task Transform_Identifies_Am_Prefixed_Segment_Ids_Async()
    {
        var payload = BuildPayload(
            $"A1610515{Fs}A2D0{Fs}A3B1",
            // AM01 = Patient
            $"AM01{Fs}C2PT-1",
            // AM02 = Pharmacy Provider
            $"AM02{Fs}NPI1234567890");
        var stage = new NcpdpTelecomTransformStage();

        var transformed = await stage.TransformAsync(BuildMessage(payload), CancellationToken.None);

        var json = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span)).RootElement;
        var segments = json.GetProperty("segments");
        Assert.Equal(3, segments.GetArrayLength());
        Assert.True(segments[0].GetProperty("segmentId").ValueKind == JsonValueKind.Null);
        Assert.Equal("AM01", segments[1].GetProperty("segmentId").GetString());
        Assert.Equal("AM02", segments[2].GetProperty("segmentId").GetString());
    }

    [Fact]
    public async Task Transform_Leaves_Non_Telecom_Payload_Untouched_Async()
    {
        // No segment separator byte → not Telecom; pass through.
        var stage = new NcpdpTelecomTransformStage();
        var message = BuildMessage("plain ASCII without any NCPDP control characters");

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        Assert.Equal(
            "plain ASCII without any NCPDP control characters",
            Encoding.UTF8.GetString(transformed.Payload.Span));
    }

    [Fact]
    public async Task Transform_Returns_Null_Transaction_Code_When_Header_Missing_A3_Async()
    {
        // Header without A3 (Transaction Code).
        var payload = BuildPayload($"A1610515{Fs}A2D0", $"AM01{Fs}C2PT-1");
        var stage = new NcpdpTelecomTransformStage();

        var transformed = await stage.TransformAsync(BuildMessage(payload), CancellationToken.None);

        var json = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span)).RootElement;
        Assert.Equal(JsonValueKind.Null, json.GetProperty("transactionCode").ValueKind);
        Assert.Equal("D0", json.GetProperty("versionRelease").GetString());
    }

    [Fact]
    public void Try_Parse_Returns_Null_For_Empty_Or_Non_Telecom_Input()
    {
        Assert.Null(NcpdpTelecomMessage.TryParse(string.Empty));
        Assert.Null(NcpdpTelecomMessage.TryParse("no separators here"));
    }

    [Fact]
    public void Try_Parse_Surfaces_Transaction_Header_Fields_On_The_Typed_Tree()
    {
        var payload = BuildPayload(
            $"A1610515{Fs}A2D0{Fs}A3E1",  // E1 = Eligibility Verification
            $"AM01{Fs}C2PT-789");

        var parsed = NcpdpTelecomMessage.TryParse(payload);

        Assert.NotNull(parsed);
        Assert.Equal("D0", parsed!.VersionRelease);
        Assert.Equal("E1", parsed.TransactionCode);
        Assert.Equal(2, parsed.Segments.Count);
        Assert.Equal("PT-789", parsed.Segments[1].GetField("C2"));
    }

    [Fact]
    public void Stage_Advertises_Ncpdp_Telecom_Kind() => Assert.Equal("ncpdp-telecom", new NcpdpTelecomTransformStage().Kind);

    private static string BuildPayload(params string[] segments) =>
        string.Join(Ss, segments) + Ss;

    private static IntegrationMessage BuildMessage(string payload) => new()
    {
        Id = Guid.CreateVersion7(),
        FlowId = Guid.CreateVersion7(),
        CorrelationId = "C",
        Payload = Encoding.UTF8.GetBytes(payload),
        PayloadFormat = PayloadFormat.Utf8Text,
        Metadata = ImmutableDictionary<string, string>.Empty,
        ReceivedAtUtc = DateTimeOffset.UtcNow,
    };
}
