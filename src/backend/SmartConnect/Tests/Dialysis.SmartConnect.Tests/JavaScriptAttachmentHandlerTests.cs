using System.Text;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Attachments.Handlers;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class JavaScriptAttachmentHandlerTests
{
    [Fact]
    public async Task Script_calls_addAttachment_and_returns_modified_payload()
    {
        await using var sp = BuildServices();
        var store = sp.GetRequiredService<IAttachmentStore>();
        var handler = new JavaScriptAttachmentHandler(sp);

        var script = """
            var token = addAttachment('blob-bytes', 'application/octet-stream');
            msg.replace('ORIG', token);
        """;
        var ctx = new AttachmentHandlerContext
        {
            FlowId = Guid.CreateVersion7(),
            MessageId = Guid.CreateVersion7(),
            ChannelMimeType = "application/octet-stream",
            PropertiesJson = System.Text.Json.JsonSerializer.Serialize(new { script }),
            Store = store,
        };

        var result = await handler.ExtractAsync(NewMessage(ctx.MessageId, ctx.FlowId, "before ORIG after"), ctx, CancellationToken.None);

        var rewritten = Encoding.UTF8.GetString(result.RewrittenPayload.Span);
        Assert.Contains("${ATTACH:", rewritten);
        Assert.DoesNotContain("ORIG", rewritten);

        var stored = await store.GetForMessageAsync(ctx.MessageId, CancellationToken.None);
        Assert.Single(stored);
        Assert.Equal("blob-bytes", Encoding.UTF8.GetString(stored[0].Data.Span));
    }

    [Fact]
    public async Task Missing_script_returns_unchanged()
    {
        await using var sp = BuildServices();
        var handler = new JavaScriptAttachmentHandler(sp);
        var ctx = new AttachmentHandlerContext
        {
            FlowId = Guid.CreateVersion7(),
            MessageId = Guid.CreateVersion7(),
            ChannelMimeType = "text/plain",
            PropertiesJson = null,
            Store = sp.GetRequiredService<IAttachmentStore>(),
        };
        var result = await handler.ExtractAsync(NewMessage(ctx.MessageId, ctx.FlowId, "x"), ctx, CancellationToken.None);
        Assert.False(result.Extracted);
    }

    private static IntegrationMessage NewMessage(Guid messageId, Guid flowId, string body) => new()
    {
        Id = messageId,
        FlowId = flowId,
        CorrelationId = "c1",
        Payload = Encoding.UTF8.GetBytes(body),
        PayloadFormat = PayloadFormat.Utf8Text,
        ReceivedAtUtc = DateTimeOffset.UtcNow,
    };

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_jsah_{Guid.NewGuid():N}");
        services.AddSmartConnectCore();
        return services.BuildServiceProvider();
    }
}
