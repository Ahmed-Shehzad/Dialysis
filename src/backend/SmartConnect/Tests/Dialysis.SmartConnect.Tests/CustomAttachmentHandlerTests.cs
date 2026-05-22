using System.Text;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Attachments.Handlers;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class CustomAttachmentHandlerTests
{
    [Fact]
    public async Task Routes_To_Registered_Custom_Kind_Async()
    {
        var registry = new MutableFlowPluginRegistry();
        registry.RegisterAttachmentHandler(new EchoHandler());
        var host = new CustomAttachmentHandlerHost(() => registry);

        var ctx = new AttachmentHandlerContext
        {
            FlowId = Guid.CreateVersion7(),
            MessageId = Guid.CreateVersion7(),
            ChannelMimeType = "text/plain",
            PropertiesJson = """{"customKind":"echo"}""",
            Store = new StubStore(),
        };

        var msg = new IntegrationMessage
        {
            Id = ctx.MessageId,
            FlowId = ctx.FlowId,
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes("hello"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
        var result = await host.ExtractAsync(msg, ctx, CancellationToken.None);
        Assert.True(result.Extracted);
        Assert.Equal("HELLO", Encoding.UTF8.GetString(result.RewrittenPayload.Span));
    }

    [Fact]
    public async Task Unknown_Kind_Returns_Unchanged_Async()
    {
        var host = new CustomAttachmentHandlerHost(() => new MutableFlowPluginRegistry());
        var ctx = new AttachmentHandlerContext
        {
            FlowId = Guid.CreateVersion7(),
            MessageId = Guid.CreateVersion7(),
            ChannelMimeType = "text/plain",
            PropertiesJson = """{"customKind":"nope"}""",
            Store = new StubStore(),
        };
        var msg = new IntegrationMessage
        {
            Id = ctx.MessageId,
            FlowId = ctx.FlowId,
            CorrelationId = "c1",
            Payload = Encoding.UTF8.GetBytes("hello"),
            PayloadFormat = PayloadFormat.Utf8Text,
            ReceivedAtUtc = DateTimeOffset.UtcNow,
        };
        var result = await host.ExtractAsync(msg, ctx, CancellationToken.None);
        Assert.False(result.Extracted);
    }

    private sealed class EchoHandler : IAttachmentHandler
    {
        public string Kind => "custom:echo";
        public Task<AttachmentHandlerResult> ExtractAsync(IntegrationMessage message, AttachmentHandlerContext context, CancellationToken cancellationToken)
        {
            var upper = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(message.Payload.Span).ToUpperInvariant());
            return Task.FromResult(new AttachmentHandlerResult { RewrittenPayload = upper, Extracted = true });
        }
    }

    private sealed class StubStore : IAttachmentStore
    {
        public Task<Attachment> AddAsync(Attachment a, CancellationToken ct) => Task.FromResult(a);
        public Attachment Add(Attachment a, CancellationToken ct) => a;
        public Task<Attachment?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult<Attachment?>(null);
        public Task<IReadOnlyList<Attachment>> GetForMessageAsync(Guid messageId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Attachment>>([]);
        public Task DeleteAsync(Guid id, CancellationToken ct) => Task.CompletedTask;
        public Task DeleteForMessageAsync(Guid messageId, CancellationToken ct) => Task.CompletedTask;
        public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken ct) => Task.FromResult(0);
    }
}
