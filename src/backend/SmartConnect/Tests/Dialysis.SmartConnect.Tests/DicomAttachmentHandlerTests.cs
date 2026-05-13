using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Attachments.Handlers;
using FellowOakDicom;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class DicomAttachmentHandlerTests
{
    [Fact]
    public async Task PixelData_extracted_default()
    {
        var pixelBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var dataset = new DicomDataset
        {
            { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage },
            { DicomTag.SOPInstanceUID, DicomUID.Generate() },
            { DicomTag.PatientID, "PID-1" },
        };
        dataset.Add(new DicomOtherByte(DicomTag.PixelData, pixelBytes));
        var file = new DicomFile(dataset);
        using var ms = new MemoryStream();
        file.Save(ms);

        var handler = new DicomAttachmentHandler();
        var ctx = new AttachmentHandlerContext
        {
            FlowId = Guid.CreateVersion7(),
            MessageId = Guid.CreateVersion7(),
            ChannelMimeType = "application/dicom",
            Store = new StubStore(),
        };

        var msg = new IntegrationMessage
        {
            Id = ctx.MessageId,
            FlowId = ctx.FlowId,
            CorrelationId = "c1",
            Payload = ms.ToArray(),
            PayloadFormat = PayloadFormat.Binary,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };

        var result = await handler.ExtractAsync(msg, ctx, CancellationToken.None);

        Assert.True(result.Extracted);
        Assert.Single(result.Attachments);
        Assert.Equal(pixelBytes, result.Attachments[0].Data.ToArray());
        Assert.Equal("application/dicom", result.Attachments[0].MimeType);
    }

    [Fact]
    public async Task Non_dicom_payload_returns_unchanged()
    {
        var handler = new DicomAttachmentHandler();
        var ctx = new AttachmentHandlerContext
        {
            FlowId = Guid.CreateVersion7(),
            MessageId = Guid.CreateVersion7(),
            ChannelMimeType = "application/dicom",
            Store = new StubStore(),
        };
        var msg = new IntegrationMessage
        {
            Id = ctx.MessageId,
            FlowId = ctx.FlowId,
            CorrelationId = "c1",
            Payload = System.Text.Encoding.UTF8.GetBytes("not dicom"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
        var result = await handler.ExtractAsync(msg, ctx, CancellationToken.None);
        Assert.False(result.Extracted);
    }

    private sealed class StubStore : IAttachmentStore
    {
        public Task<Attachment> AddAsync(Attachment a, CancellationToken ct) => Task.FromResult(a);
        public Task<Attachment?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult<Attachment?>(null);
        public Task<IReadOnlyList<Attachment>> GetForMessageAsync(Guid messageId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Attachment>>([]);
        public Task DeleteAsync(Guid id, CancellationToken ct) => Task.CompletedTask;
        public Task DeleteForMessageAsync(Guid messageId, CancellationToken ct) => Task.CompletedTask;
        public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken ct) => Task.FromResult(0);
    }
}
