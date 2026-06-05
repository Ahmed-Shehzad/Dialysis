using System.Text;
using Dialysis.SmartConnect.Iteration;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class IterableResolverTests
{
    [Fact]
    public void Hl7_Segment_Iteration_Yields_Each_Obx()
    {
        var msg = Wrap_Hl7(
            "MSH|^~\\&|SRC|FAC|DEST|FAC|202601010000||ORU^R01|1|P|2.5\r" +
            "PID|||MRN-1\r" +
            "OBX|1|NM|1234^^MDC||145.5||mm[Hg]\r" +
            "OBX|2|NM|1235^^MDC||80||mm[Hg]\r" +
            "OBX|3|NM|1236^^MDC||72||bpm");

        var elements = IterableResolver.Resolve(msg, "OBX");

        Assert.Equal(3, elements.Count);
        Assert.StartsWith("OBX|1", elements[0].Value);
        Assert.StartsWith("OBX|2", elements[1].Value);
        Assert.StartsWith("OBX|3", elements[2].Value);
    }

    [Fact]
    public void Hl7_Field_Repeats_Iterate_Pid_3()
    {
        var msg = Wrap_Hl7(
            "MSH|^~\\&|SRC|FAC|DEST|FAC|202601010000||ORU^R01|1|P|2.5\r" +
            "PID|||MRN-1~MRN-2~MRN-3||Doe^Jane");

        var elements = IterableResolver.Resolve(msg, "PID.3");

        Assert.Equal(3, elements.Count);
        Assert.Equal("MRN-1", elements[0].Value);
        Assert.Equal("MRN-2", elements[1].Value);
        Assert.Equal("MRN-3", elements[2].Value);
    }

    [Fact]
    public void Json_Array_Iteration()
    {
        var msg = Wrap_Json("""{"observations":[{"v":1},{"v":2},{"v":3}]}""");

        var elements = IterableResolver.Resolve(msg, "$.observations[*]");

        Assert.Equal(3, elements.Count);
        Assert.Contains("\"v\":1", elements[0].Value);
        Assert.Contains("\"v\":2", elements[1].Value);
        Assert.Contains("\"v\":3", elements[2].Value);
    }

    [Fact]
    public void Xml_Xpath_Iteration()
    {
        var msg = Wrap_Xml("<order><items><item>A</item><item>B</item></items></order>");

        var elements = IterableResolver.Resolve(msg, "/order/items/item");

        Assert.Equal(2, elements.Count);
        Assert.Contains("A", elements[0].Value);
        Assert.Contains("B", elements[1].Value);
    }

    [Fact]
    public void Missing_Path_Yields_Empty()
    {
        var msg = Wrap_Json("""{"a":1}""");
        var elements = IterableResolver.Resolve(msg, "$.b[*]");
        Assert.Empty(elements);
    }

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

    private static IntegrationMessage Wrap_Xml(string payload) =>
        new()
        {
            Id = Guid.NewGuid(),
            FlowId = Guid.NewGuid(),
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes(payload),
            PayloadFormat = PayloadFormat.PlainText,
            Metadata = [],
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
}
