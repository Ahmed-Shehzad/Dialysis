using System.Collections.Immutable;
using System.Text;
using System.Xml.Linq;
using Dialysis.SmartConnect.Transforms;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Transforms;

/// <summary>
/// Covers the new <c>hl7v2-to-xml</c> mode on the <c>xml-transform</c> stage. Existing XPath-only
/// behaviour is preserved by other tests; these focus on the HL7 → XML projection plus the
/// combined "HL7 + XPath" drill.
/// </summary>
public sealed class XmlTransformStageHl7Tests
{
    private const string AdtSample =
        "MSH|^~\\&|HIS|HOSPITAL|EMR|CLINIC|20260526120000||ADT^A01|MSG-1|P|2.5\r" +
        "PID|||MRN-12345||DOE^JOHN";

    [Fact]
    public async Task Mode_Hl7v2_To_Xml_Emits_Structured_Document_Async()
    {
        var stage = new XmlTransformStage();
        var message = NewMessage(AdtSample, """{"mode":"hl7v2-to-xml"}""");

        var result = await stage.TransformAsync(message, CancellationToken.None);
        var xml = XDocument.Parse(Encoding.UTF8.GetString(result.Payload.Span));

        Assert.Equal("HL7Message", xml.Root?.Name.LocalName);
        Assert.NotNull(xml.Root?.Element("MSH"));
        Assert.NotNull(xml.Root?.Element("PID"));
    }

    [Fact]
    public async Task Mode_Hl7v2_To_Xml_With_X_Path_Extracts_Field_Async()
    {
        var stage = new XmlTransformStage();
        var message = NewMessage(AdtSample, """{"mode":"hl7v2-to-xml","xpath":"string(/HL7Message/PID/F3)"}""");

        var result = await stage.TransformAsync(message, CancellationToken.None);
        var value = Encoding.UTF8.GetString(result.Payload.Span);

        Assert.Equal("MRN-12345", value);
    }

    [Fact]
    public async Task Mode_Hl7v2_To_Xml_Skips_Non_Hl7_Payload_Async()
    {
        var stage = new XmlTransformStage();
        var message = NewMessage("not hl7", """{"mode":"hl7v2-to-xml"}""");

        var result = await stage.TransformAsync(message, CancellationToken.None);

        Assert.Equal(message.Payload.ToArray(), result.Payload.ToArray());
    }

    [Fact]
    public async Task Existing_X_Path_Only_Mode_Still_Works_For_Xml_Input_Async()
    {
        var stage = new XmlTransformStage();
        var message = NewMessage("<root><foo>bar</foo></root>", """{"xpath":"string(/root/foo)"}""");

        var result = await stage.TransformAsync(message, CancellationToken.None);
        Assert.Equal("bar", Encoding.UTF8.GetString(result.Payload.Span));
    }

    private static IntegrationMessage NewMessage(string payload, string parametersJson) => new()
    {
        Id = Guid.CreateVersion7(),
        FlowId = Guid.CreateVersion7(),
        CorrelationId = "corr-" + Guid.NewGuid().ToString("N")[..8],
        Payload = Encoding.UTF8.GetBytes(payload),
        PayloadFormat = PayloadFormat.Utf8Text,
        Metadata = ImmutableDictionary<string, string>.Empty.Add(
            "smartconnect.transform.parameters",
            parametersJson),
        ReceivedAtUtc = DateTimeOffset.UtcNow,
    };
}
