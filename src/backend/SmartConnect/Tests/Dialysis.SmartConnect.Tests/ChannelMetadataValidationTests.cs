using Dialysis.SmartConnect.Management.AspNetCore;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Covers <see cref="PipelineValidation.ValidateChannelMetadataOrThrow"/> for the new channel-
/// level metadata fields (dataTypes enum, dependency self-reference, attachment size cap).
/// </summary>
public sealed class ChannelMetadataValidationTests
{
    [Fact]
    public void Accepts_Empty_Metadata()
    {
        var flow = NewFlow();
        PipelineValidation.ValidateChannelMetadataOrThrow(flow);
    }

    [Theory]
    [InlineData("HL7v2")]
    [InlineData("FHIR")]
    [InlineData("NCPDP")]
    [InlineData("JSON")]
    [InlineData("XML")]
    [InlineData("Binary")]
    [InlineData("Other")]
    [InlineData("hl7v2")] // case-insensitive
    public void Accepts_Known_Data_Type(string value)
    {
        var flow = NewFlow(dataTypes: [value]);
        PipelineValidation.ValidateChannelMetadataOrThrow(flow);
    }

    [Fact]
    public void Rejects_Unknown_Data_Type()
    {
        var flow = NewFlow(dataTypes: ["WireOnly"]);
        Assert.Throws<InvalidOperationException>(() => PipelineValidation.ValidateChannelMetadataOrThrow(flow));
    }

    [Fact]
    public void Rejects_Self_Dependency()
    {
        var id = Guid.NewGuid();
        var flow = NewFlow(id: id, dependencies: [id]);
        Assert.Throws<InvalidOperationException>(() => PipelineValidation.ValidateChannelMetadataOrThrow(flow));
    }

    [Fact]
    public void Rejects_Attachment_Without_Name()
    {
        var flow = NewFlow(attachments:
        [
            new ChannelAttachmentReference { Name = "", MimeType = "text/plain", Base64Bytes = "AAA=" },
        ]);
        Assert.Throws<InvalidOperationException>(() => PipelineValidation.ValidateChannelMetadataOrThrow(flow));
    }

    [Fact]
    public void Rejects_Attachment_Over_Size_Cap()
    {
        // 1.6 MiB base64 string (encodes ~1.2 MiB) — over the 1 MiB decoded cap.
        var oversize = new string('A', 1_600_000);
        var flow = NewFlow(attachments:
        [
            new ChannelAttachmentReference { Name = "sample.hl7", MimeType = "application/hl7-v2", Base64Bytes = oversize },
        ]);
        Assert.Throws<InvalidOperationException>(() => PipelineValidation.ValidateChannelMetadataOrThrow(flow));
    }

    private static IntegrationFlow NewFlow(
        Guid? id = null,
        IReadOnlyList<string>? dataTypes = null,
        IReadOnlyList<Guid>? dependencies = null,
        IReadOnlyList<ChannelAttachmentReference>? attachments = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "test-flow",
        RuntimeState = FlowRuntimeState.Stopped,
        Pipeline = new IntegrationFlowPipelineDefinition(),
        DataTypes = [.. (dataTypes ?? [])],
        Dependencies = [.. (dependencies ?? [])],
        Attachments = [.. (attachments ?? [])],
    };
}
