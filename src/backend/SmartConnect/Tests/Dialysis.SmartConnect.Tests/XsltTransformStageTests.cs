using System.Text;
using Dialysis.SmartConnect.ExtendedPlugins;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class XsltTransformStageTests
{
    private const string SimpleStylesheet = """
        <?xml version="1.0" encoding="UTF-8"?>
        <xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
          <xsl:output method="xml" indent="no" omit-xml-declaration="yes"/>
          <xsl:template match="/root">
            <output><xsl:value-of select="value"/></output>
          </xsl:template>
        </xsl:stylesheet>
        """;

    [Fact]
    public async Task Transform_applies_xslt_stylesheet()
    {
        var stage = new XsltTransformStage();
        var xml = "<root><value>hello</value></root>";
        var msg = CreateMessage($$"""{"stylesheet":{{EscapeJson(SimpleStylesheet)}}}""", xml);

        var result = await stage.TransformAsync(msg, CancellationToken.None);

        var output = Encoding.UTF8.GetString(result.Payload.Span);
        Assert.Contains("<output>hello</output>", output);
    }

    [Fact]
    public async Task Transform_without_parameters_passes_through()
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
    public async Task Transform_caches_compiled_stylesheets()
    {
        var stage = new XsltTransformStage();
        var xml = "<root><value>v</value></root>";
        var paramJson = $$"""{"stylesheet":{{EscapeJson(SimpleStylesheet)}}}""";
        var msg1 = CreateMessage(paramJson, xml);
        var msg2 = CreateMessage(paramJson, xml);

        // Both should succeed (second uses cache)
        var r1 = await stage.TransformAsync(msg1, CancellationToken.None);
        var r2 = await stage.TransformAsync(msg2, CancellationToken.None);

        Assert.Contains("<output>v</output>", Encoding.UTF8.GetString(r1.Payload.Span));
        Assert.Contains("<output>v</output>", Encoding.UTF8.GetString(r2.Payload.Span));
    }

    [Fact]
    public async Task Transform_throws_on_invalid_xml()
    {
        var stage = new XsltTransformStage();
        var msg = CreateMessage($$"""{"stylesheet":{{EscapeJson(SimpleStylesheet)}}}""", "not xml at all");

        await Assert.ThrowsAnyAsync<Exception>(() => stage.TransformAsync(msg, CancellationToken.None));
    }

    private static IntegrationMessage CreateMessage(string parametersJson, string payload)
    {
        return new IntegrationMessage
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes(payload),
            PayloadFormat = PayloadFormat.Utf8Text,
            Metadata = System.Collections.Immutable.ImmutableDictionary<string, string>.Empty.Add(
                XsltTransformStage.ParametersMetadataKey,
                parametersJson),
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    private static string EscapeJson(string s) =>
        System.Text.Json.JsonSerializer.Serialize(s);
}
