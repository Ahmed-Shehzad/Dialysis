using System.Text;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

public sealed class AttachmentStoreTests
{
    [Fact]
    public async Task Add_Then_Get_Round_Trips_Bytes_And_Metadata_Async()
    {
        await using var sp = Build_Services();
        var store = sp.GetRequiredService<IAttachmentStore>();

        var bytes = Encoding.UTF8.GetBytes("hello");
        var added = await store.AddAsync(new Attachment
        {
            Id = Guid.CreateVersion7(),
            MessageId = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            MimeType = "text/plain",
            Data = bytes,
            SizeBytes = bytes.Length,
            CreatedUtc = DateTimeOffset.UtcNow,
        }, CancellationToken.None);

        var fetched = await store.GetAsync(added.Id, CancellationToken.None);
        Assert.NotNull(fetched);
        Assert.Equal("text/plain", fetched!.MimeType);
        Assert.Equal("hello", Encoding.UTF8.GetString(fetched.Data.Span));
    }

    [Fact]
    public async Task Getformessage_Returns_All_For_That_Message_Async()
    {
        await using var sp = Build_Services();
        var store = sp.GetRequiredService<IAttachmentStore>();
        var messageId = Guid.CreateVersion7();

        for (var i = 0; i < 3; i++)
        {
            await store.AddAsync(new Attachment
            {
                Id = Guid.CreateVersion7(),
                MessageId = messageId,
                FlowId = Guid.CreateVersion7(),
                MimeType = "text/plain",
                Data = Encoding.UTF8.GetBytes($"row-{i}"),
                SizeBytes = 5,
                CreatedUtc = DateTimeOffset.UtcNow,
            }, CancellationToken.None);
        }
        await store.AddAsync(new Attachment
        {
            Id = Guid.CreateVersion7(),
            MessageId = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            MimeType = "text/plain",
            Data = Encoding.UTF8.GetBytes("other"),
            SizeBytes = 5,
            CreatedUtc = DateTimeOffset.UtcNow,
        }, CancellationToken.None);

        var list = await store.GetForMessageAsync(messageId, CancellationToken.None);
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public async Task Deleteformessage_Cascades_Async()
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
            Data = Encoding.UTF8.GetBytes("x"),
            SizeBytes = 1,
            CreatedUtc = DateTimeOffset.UtcNow,
        }, CancellationToken.None);

        await store.DeleteForMessageAsync(messageId, CancellationToken.None);
        Assert.Null(await store.GetAsync(added.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Deleteolderthan_Removes_Only_Old_Rows_Async()
    {
        await using var sp = Build_Services();
        var store = sp.GetRequiredService<IAttachmentStore>();
        var older = await store.AddAsync(new Attachment
        {
            Id = Guid.CreateVersion7(),
            MessageId = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            MimeType = "text/plain",
            Data = Encoding.UTF8.GetBytes("old"),
            SizeBytes = 3,
            CreatedUtc = DateTimeOffset.UtcNow.AddDays(-30),
        }, CancellationToken.None);
        var newer = await store.AddAsync(new Attachment
        {
            Id = Guid.CreateVersion7(),
            MessageId = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7(),
            MimeType = "text/plain",
            Data = Encoding.UTF8.GetBytes("new"),
            SizeBytes = 3,
            CreatedUtc = DateTimeOffset.UtcNow,
        }, CancellationToken.None);

        var removed = await store.DeleteOlderThanAsync(DateTimeOffset.UtcNow.AddDays(-7), CancellationToken.None);
        Assert.Equal(1, removed);
        Assert.Null(await store.GetAsync(older.Id, CancellationToken.None));
        Assert.NotNull(await store.GetAsync(newer.Id, CancellationToken.None));
    }

    private static ServiceProvider Build_Services()
    {
        var services = new ServiceCollection();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        return services.BuildServiceProvider();
    }
}
