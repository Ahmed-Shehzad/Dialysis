using System.Text;
using System.Text.Json;
using Dialysis.SmartConnect.ExtendedPlugins;
using FellowOakDicom;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Transforms;

/// <summary>
/// Slice E: the DICOM transform stage parses a DICOM payload into a JSON-addressable form
/// so downstream JSON-path / mapper / JavaScript transforms can read tag values without
/// writing custom parsers. Pixel data stays out by default (it belongs in attachments via
/// the existing <c>DicomAttachmentHandler</c>) so the ledger doesn't bloat.
/// </summary>
public sealed class DicomTransformStageTests
{
    [Fact]
    public async Task Transform_Projects_Tag_Names_To_Json_By_Default_Async()
    {
        var message = await Build_Dicom_Message_Async();
        var stage = new DicomTransformStage();

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span));
        Assert.Equal("MRN-12345", doc.RootElement.GetProperty("PatientID").GetString());
        Assert.Equal("Doe^John", doc.RootElement.GetProperty("PatientName").GetString());
        // Pixel data must NOT bleed into the JSON projection by default.
        Assert.False(doc.RootElement.TryGetProperty("PixelData", out _));
    }

    [Fact]
    public async Task Transform_Emits_Hex_Tag_Keys_When_Format_Is_By_Tag_Async()
    {
        var message = (await Build_Dicom_Message_Async())
            .WithMetadata(DicomTransformStage.ParametersMetadataKey,
                """{"format":"by-tag"}""");
        var stage = new DicomTransformStage();

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span));
        // PatientID = (0010,0020), PatientName = (0010,0010); both keys hex-encoded.
        Assert.Equal("MRN-12345", doc.RootElement.GetProperty("00100020").GetString());
        Assert.Equal("Doe^John", doc.RootElement.GetProperty("00100010").GetString());
    }

    [Fact]
    public async Task Transform_Restricts_Output_To_Include_Tags_When_Specified_Async()
    {
        var message = (await Build_Dicom_Message_Async())
            .WithMetadata(DicomTransformStage.ParametersMetadataKey,
                """{"includeTags":["00100020"]}""");
        var stage = new DicomTransformStage();

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span));
        Assert.True(doc.RootElement.TryGetProperty("PatientID", out _));
        // PatientName excluded by the include filter.
        Assert.False(doc.RootElement.TryGetProperty("PatientName", out _));
    }

    [Fact]
    public async Task Transform_Leaves_Message_Unchanged_When_Payload_Is_Not_Dicom_Async()
    {
        var message = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            CorrelationId = "C",
            Payload = "not a dicom file"u8.ToArray(),
            PayloadFormat = PayloadFormat.Utf8Text,
            Metadata = [],
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
        var stage = new DicomTransformStage();

        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        // Failed parse → pass through unchanged so the downstream route still sees the
        // original payload and can decide how to handle the unexpected format.
        Assert.Equal("not a dicom file", Encoding.UTF8.GetString(transformed.Payload.Span));
    }

    [Fact]
    public void Transform_Advertises_Dicom_Kind()
    {
        var stage = new DicomTransformStage();
        Assert.Equal("dicom", stage.Kind);
    }

    [Fact]
    public async Task Transform_Projects_Sequence_Items_As_Nested_Arrays_Async()
    {
        // Slice E2: a DicomSequence element should project as a JSON array of objects,
        // one per sequence item. Earlier behaviour silently skipped SQ elements.
        var sequenceItem1 = new DicomDataset
        {
            { DicomTag.RequestedProcedureID, "RP-001" },
            { DicomTag.AccessionNumber, "ACC-A" },
        };
        var sequenceItem2 = new DicomDataset
        {
            { DicomTag.RequestedProcedureID, "RP-002" },
            { DicomTag.AccessionNumber, "ACC-B" },
        };
        var dataset = new DicomDataset
        {
            { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage },
            { DicomTag.SOPInstanceUID, DicomUID.Generate() },
            { DicomTag.PatientID, "MRN-1" },
            new DicomSequence(DicomTag.RequestAttributesSequence, sequenceItem1, sequenceItem2),
        };
        var file = new DicomFile(dataset);
        using var ms = new MemoryStream();
        await file.SaveAsync(ms);

        var message = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            CorrelationId = "C",
            Payload = ms.ToArray(),
            PayloadFormat = PayloadFormat.Binary,
            Metadata = [],
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var stage = new DicomTransformStage();
        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span));
        var sequence = doc.RootElement.GetProperty("RequestAttributesSequence");
        Assert.Equal(JsonValueKind.Array, sequence.ValueKind);
        Assert.Equal(2, sequence.GetArrayLength());
        Assert.Equal("RP-001", sequence[0].GetProperty("RequestedProcedureID").GetString());
        Assert.Equal("ACC-A", sequence[0].GetProperty("AccessionNumber").GetString());
        Assert.Equal("RP-002", sequence[1].GetProperty("RequestedProcedureID").GetString());
    }

    [Fact]
    public async Task Transform_Returns_Empty_Sequence_Array_When_Sequence_Has_No_Items_Async()
    {
        var dataset = new DicomDataset
        {
            { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage },
            { DicomTag.SOPInstanceUID, DicomUID.Generate() },
            { DicomTag.PatientID, "MRN-empty" },
            new DicomSequence(DicomTag.RequestAttributesSequence),
        };
        var file = new DicomFile(dataset);
        using var ms = new MemoryStream();
        await file.SaveAsync(ms);

        var message = new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            CorrelationId = "C",
            Payload = ms.ToArray(),
            PayloadFormat = PayloadFormat.Binary,
            Metadata = [],
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var stage = new DicomTransformStage();
        var transformed = await stage.TransformAsync(message, CancellationToken.None);

        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(transformed.Payload.Span));
        var sequence = doc.RootElement.GetProperty("RequestAttributesSequence");
        Assert.Equal(JsonValueKind.Array, sequence.ValueKind);
        Assert.Equal(0, sequence.GetArrayLength());
    }

    private static async Task<IntegrationMessage> Build_Dicom_Message_Async()
    {
        var pixelBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var dataset = new DicomDataset
        {
            { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage },
            { DicomTag.SOPInstanceUID, DicomUID.Generate() },
            { DicomTag.PatientID, "MRN-12345" },
            { DicomTag.PatientName, "Doe^John" },
        };
        dataset.Add(new DicomOtherByte(DicomTag.PixelData, pixelBytes));
        var file = new DicomFile(dataset);
        using var ms = new MemoryStream();
        await file.SaveAsync(ms);

        return new IntegrationMessage
        {
            Id = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            CorrelationId = "C",
            Payload = ms.ToArray(),
            PayloadFormat = PayloadFormat.Binary,
            Metadata = [],
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}
