using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.ExtendedPlugins;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class XsltTransformStageTests
{
    private const string Simplestylesheet = """
        <?xml version="1.0" encoding="UTF-8"?>
        <xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
          <xsl:output method="xml" indent="no" omit-xml-declaration="yes"/>
          <xsl:template match="/root">
            <output><xsl:value-of select="value"/></output>
          </xsl:template>
        </xsl:stylesheet>
        """;

    [Fact]
    public async Task Transform_Applies_Xslt_Stylesheet_Async()
    {
        var stage = new XsltTransformStage();
        var xml = "<root><value>hello</value></root>";
        var msg = Create_Message($$"""{"stylesheet":{{Escape_Json(Simplestylesheet)}}}""", xml);

        var result = await stage.TransformAsync(msg, CancellationToken.None);

        var output = Encoding.UTF8.GetString(result.Payload.Span);
        Assert.Contains("<output>hello</output>", output);
    }

    [Fact]
    public async Task Transform_Without_Parameters_Passes_Through_Async()
    {
        var stage = new XsltTransformStage();
        var msg = new IntegrationMessage
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "c1",
            Payload = "<root/>"u8.ToArray(),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await stage.TransformAsync(msg, CancellationToken.None);

        Assert.Equal(msg.Payload.ToArray(), result.Payload.ToArray());
    }

    [Fact]
    public async Task Transform_Caches_Compiled_Stylesheets_Async()
    {
        var stage = new XsltTransformStage();
        var xml = "<root><value>v</value></root>";
        var paramJson = $$"""{"stylesheet":{{Escape_Json(Simplestylesheet)}}}""";
        var msg1 = Create_Message(paramJson, xml);
        var msg2 = Create_Message(paramJson, xml);

        // Both should succeed (second uses cache)
        var r1 = await stage.TransformAsync(msg1, CancellationToken.None);
        var r2 = await stage.TransformAsync(msg2, CancellationToken.None);

        Assert.Contains("<output>v</output>", Encoding.UTF8.GetString(r1.Payload.Span));
        Assert.Contains("<output>v</output>", Encoding.UTF8.GetString(r2.Payload.Span));
    }

    [Fact]
    public async Task Transform_Throws_On_Invalid_Xml_Async()
    {
        var stage = new XsltTransformStage();
        var msg = Create_Message($$"""{"stylesheet":{{Escape_Json(Simplestylesheet)}}}""", "not xml at all");

        await Assert.ThrowsAnyAsync<Exception>(() => stage.TransformAsync(msg, CancellationToken.None));
    }

    private static IntegrationMessage Create_Message(string parametersJson, string payload)
    {
        return new IntegrationMessage
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes(payload),
            PayloadFormat = PayloadFormat.Utf8Text,
            Metadata = ImmutableDictionary<string, string>.Empty.Add(
                XsltTransformStage.ParametersMetadataKey,
                parametersJson),
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    private static string Escape_Json(string s) =>
        JsonSerializer.Serialize(s);
}
