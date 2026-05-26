using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.DataTypes;
using Dialysis.SmartConnect.Transforms;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// HL7 v2 has minor encoding differences from v2.1 → v2.8+, but the field-pipe / caret-component /
/// tilde-repeat encoding is unchanged across all versions in production use. This matrix locks in
/// "Parse + read MSH.12 round-trips" for the headline versions we promise to support, and confirms
/// the verify-hl7 minVersion filter selects correctly.
/// </summary>
public sealed class Hl7VersionMatrixTests
{
    [Theory]
    [InlineData("2.1", "MSH|^~\\&|S|F|D|FA|20260101010101||ADT^A01|MSGID1|P|2.1\rEVN|A01|20260101010101\rPID|1||MRN1||DOE^J||19800101|M")]
    [InlineData("2.3", "MSH|^~\\&|S|F|D|FA|20260101010101||ADT^A01|MSGID2|P|2.3\rEVN|A01|20260101010101\rPID|1||MRN2||DOE^J||19800101|M")]
    [InlineData("2.3.1", "MSH|^~\\&|S|F|D|FA|20260101010101||ADT^A01|MSGID2b|P|2.3.1\rEVN|A01|20260101010101\rPID|1||MRN2b||DOE^J||19800101|M")]
    [InlineData("2.5", "MSH|^~\\&|S|F|D|FA|20260101010101||ADT^A01|MSGID3|P|2.5\rEVN|A01|20260101010101\rPID|1||MRN3||DOE^J||19800101|M")]
    [InlineData("2.5.1", "MSH|^~\\&|S|F|D|FA|20260101010101||ADT^A01|MSGID3b|P|2.5.1\rEVN|A01|20260101010101\rPID|1||MRN3b||DOE^J||19800101|M")]
    [InlineData("2.6", "MSH|^~\\&|S|F|D|FA|20260101010101||ADT^A01|MSGID3c|P|2.6\rEVN|A01|20260101010101\rPID|1||MRN3c||DOE^J||19800101|M")]
    [InlineData("2.7", "MSH|^~\\&|S|F|D|FA|20260101010101||ADT^A01|MSGID4|P|2.7\rEVN|A01|20260101010101\rPID|1||MRN4||DOE^J||19800101|M")]
    [InlineData("2.8", "MSH|^~\\&|S|F|D|FA|20260101010101||ADT^A01|MSGID5|P|2.8\rEVN|A01|20260101010101\rPID|1||MRN5||DOE^J||19800101|M")]
    public void Parser_Round_Trips_Msh_Version(string version, string payload)
    {
        var parsed = Hl7V2Message.Parse(payload);

        Assert.Equal(version, parsed.GetValue("MSH.12"));
        // Sanity: the surrounding segments came through too.
        Assert.NotNull(parsed.GetValue("PID.3"));
        Assert.Equal("A01", parsed.GetValue("MSH.9.2"));
    }

    [Theory]
    [InlineData("2.1", false)]
    [InlineData("2.3", false)]
    [InlineData("2.3.1", false)]
    [InlineData("2.5", true)]
    [InlineData("2.5.1", true)]
    [InlineData("2.6", true)]
    [InlineData("2.7", true)]
    [InlineData("2.8", true)]
    public async Task Verify_Hl7_Min_Version_2_5_Accepts_2_5_Plus_And_Rejects_Older_Async(string version, bool shouldPass)
    {
        var payload = $"MSH|^~\\&|S|F|D|FA|20260101010101||ADT^A01|MSGID|P|{version}\rEVN|A01|20260101010101\rPID|1||MRN||DOE^J||19800101|M";
        var paramsJson = JsonSerializer.Serialize(new { minVersion = "2.5" });
        var msg = BuildMessage(payload, paramsJson);

        var filter = new VerifyHl7RouteFilter();
        var verdict = await filter.EvaluateAsync(msg, CancellationToken.None);

        Assert.Equal(shouldPass, verdict.Disposition == RouteFilterDisposition.Allow);
    }

    private static IntegrationMessage BuildMessage(string payloadText, string filterParametersJson)
    {
        var metadata = ImmutableDictionary<string, string>.Empty
            .Add("smartconnect.filter.parameters", filterParametersJson);
        return new IntegrationMessage
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "version-matrix",
            Payload = Encoding.UTF8.GetBytes(payloadText),
            PayloadFormat = PayloadFormat.Utf8Text,
            Metadata = metadata,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}
