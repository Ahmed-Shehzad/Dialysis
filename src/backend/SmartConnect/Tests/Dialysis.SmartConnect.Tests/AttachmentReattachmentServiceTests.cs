using System.Text;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class AttachmentReattachmentServiceTests
{
    [Fact]
    public async Task Single_Token_Inflates_To_Bytes_Async()
    {
        await using var sp = Build_Services();
        var store = sp.GetRequiredService<IAttachmentStore>();
        var messageId = Guid.CreateVersion7();

        var added = await store.AddAsync(new Attachment
        {
            Id = Guid.CreateVersion7(),
            MessageId = messageId,
            FlowId = Guid.CreateVersion7(),
            MimeType = "text/plain",
            Data = Encoding.UTF8.GetBytes("INFLATED"),
            SizeBytes = 8,
            CreatedUtc = DateTimeOffset.UtcNow,
        }, CancellationToken.None);

        var service = new AttachmentReattachmentService(store);
        var payload = Encoding.UTF8.GetBytes($"pre {AttachmentReference.Format(added.Id)} post");
        var inflated = await service.InflateAsync(payload, messageId, CancellationToken.None);

        Assert.Equal("pre INFLATED post", Encoding.UTF8.GetString(inflated.Span));
    }

    [Fact]
    public async Task Missing_Attachment_Leaves_Token_Intact_Async()
    {
        await using var sp = Build_Services();
        var store = sp.GetRequiredService<IAttachmentStore>();
        var service = new AttachmentReattachmentService(store);

        var token = AttachmentReference.Format(Guid.CreateVersion7());
        var payload = Encoding.UTF8.GetBytes($"head {token} tail");
        var inflated = await service.InflateAsync(payload, Guid.CreateVersion7(), CancellationToken.None);
        Assert.Equal($"head {token} tail", Encoding.UTF8.GetString(inflated.Span));
    }

    [Fact]
    public async Task No_Tokens_Returns_Payload_Unchanged_Async()
    {
        await using var sp = Build_Services();
        var service = new AttachmentReattachmentService(sp.GetRequiredService<IAttachmentStore>());

        var payload = Encoding.UTF8.GetBytes("nothing to inflate");
        var inflated = await service.InflateAsync(payload, Guid.CreateVersion7(), CancellationToken.None);
        Assert.Equal("nothing to inflate", Encoding.UTF8.GetString(inflated.Span));
    }

    private static ServiceProvider Build_Services()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceInMemory(databaseName: $"sc_reattach_{Guid.NewGuid():N}");
        return services.BuildServiceProvider();
    }
}
