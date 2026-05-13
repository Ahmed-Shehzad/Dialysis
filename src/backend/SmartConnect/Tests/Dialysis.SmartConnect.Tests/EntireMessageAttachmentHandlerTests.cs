using System.Text;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Attachments.Handlers;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class EntireMessageAttachmentHandlerTests
{
    [Fact]
    public async Task Entire_payload_becomes_one_attachment_with_token_payload()
    {
        var handler = new EntireMessageAttachmentHandler();
        var bytes = Encoding.UTF8.GetBytes("the whole message");
        var msg = NewMessage(bytes);

        var ctx = new AttachmentHandlerContext
        {
            FlowId = msg.FlowId,
            MessageId = msg.Id,
            ChannelMimeType = "text/plain",
            Store = new StubStore(),
        };
        var result = await handler.ExtractAsync(msg, ctx, CancellationToken.None);

        Assert.True(result.Extracted);
        Assert.Single(result.Attachments);
        Assert.Equal("the whole message", Encoding.UTF8.GetString(result.Attachments[0].Data.Span));
        Assert.Equal("text/plain", result.Attachments[0].MimeType);
        Assert.StartsWith("${ATTACH:", Encoding.UTF8.GetString(result.RewrittenPayload.Span));
    }

    [Fact]
    public async Task Properties_override_mimeType()
    {
        var handler = new EntireMessageAttachmentHandler();
        var ctx = new AttachmentHandlerContext
        {
            FlowId = Guid.CreateVersion7(),
            MessageId = Guid.CreateVersion7(),
            ChannelMimeType = "application/octet-stream",
            PropertiesJson = """{"mimeType":"application/pdf"}""",
            Store = new StubStore(),
        };
        var result = await handler.ExtractAsync(NewMessage(Encoding.UTF8.GetBytes("x")), ctx, CancellationToken.None);
        Assert.Equal("application/pdf", result.Attachments[0].MimeType);
    }

    private static IntegrationMessage NewMessage(byte[] payload) => new()
    {
        Id = Guid.CreateVersion7(),
        FlowId = Guid.CreateVersion7(),
        CorrelationId = "c1",
        Payload = payload,
        PayloadFormat = PayloadFormat.Utf8Text,
        ReceivedAtUtc = DateTimeOffset.UtcNow,
    };

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
