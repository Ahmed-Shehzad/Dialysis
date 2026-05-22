using System.Text;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Attachments.Handlers;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class RegexAttachmentHandlerTests
{
    [Fact]
    public async Task Extracts_Each_Capture_Group_As_Separate_Attachment_Async()
    {
        var handler = new RegexAttachmentHandler();
        var payload = Encoding.UTF8.GetBytes("PRE[AAA]MID[BBB]POST");
        var ctx = new AttachmentHandlerContext
        {
            FlowId = Guid.CreateVersion7(),
            MessageId = Guid.CreateVersion7(),
            ChannelMimeType = "text/plain",
            PropertiesJson = """{"pattern":"\\[([^\\]]+)\\]","mimeType":"application/octet-stream"}""",
            Store = new StubStore(),
        };

        var result = await handler.ExtractAsync(New_Message(payload), ctx, CancellationToken.None);

        Assert.True(result.Extracted);
        Assert.Equal(2, result.Attachments.Count);
        Assert.Equal("AAA", Encoding.UTF8.GetString(result.Attachments[0].Data.Span));
        Assert.Equal("BBB", Encoding.UTF8.GetString(result.Attachments[1].Data.Span));

        var rewrittenText = Encoding.UTF8.GetString(result.RewrittenPayload.Span);
        Assert.DoesNotContain("AAA", rewrittenText);
        Assert.DoesNotContain("BBB", rewrittenText);
        Assert.Contains("${ATTACH:", rewrittenText);
        Assert.Equal(2, rewrittenText.Split("${ATTACH:").Length - 1);
    }

    [Fact]
    public async Task No_Matches_Leaves_Payload_Untouched_Async()
    {
        var handler = new RegexAttachmentHandler();
        var payload = Encoding.UTF8.GetBytes("nothing to match");
        var ctx = new AttachmentHandlerContext
        {
            FlowId = Guid.CreateVersion7(),
            MessageId = Guid.CreateVersion7(),
            ChannelMimeType = "text/plain",
            PropertiesJson = """{"pattern":"\\[(\\d+)\\]"}""",
            Store = new StubStore(),
        };

        var result = await handler.ExtractAsync(New_Message(payload), ctx, CancellationToken.None);
        Assert.False(result.Extracted);
    }

    [Fact]
    public async Task Empty_Pattern_Returns_Unchanged_Async()
    {
        var handler = new RegexAttachmentHandler();
        var ctx = new AttachmentHandlerContext
        {
            FlowId = Guid.CreateVersion7(),
            MessageId = Guid.CreateVersion7(),
            ChannelMimeType = "text/plain",
            PropertiesJson = "{}",
            Store = new StubStore(),
        };
        var result = await handler.ExtractAsync(New_Message(Encoding.UTF8.GetBytes("xx")), ctx, CancellationToken.None);
        Assert.False(result.Extracted);
    }

    private static IntegrationMessage New_Message(byte[] payload) => new()
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
        public Attachment Add(Attachment a, CancellationToken ct) => a;
        public Task<Attachment?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult<Attachment?>(null);
        public Task<IReadOnlyList<Attachment>> GetForMessageAsync(Guid messageId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Attachment>>([]);
        public Task DeleteAsync(Guid id, CancellationToken ct) => Task.CompletedTask;
        public Task DeleteForMessageAsync(Guid messageId, CancellationToken ct) => Task.CompletedTask;
        public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken ct) => Task.FromResult(0);
    }
}
