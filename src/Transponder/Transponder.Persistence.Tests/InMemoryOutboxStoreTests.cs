using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.Tests;

public sealed class InMemoryOutboxStoreTests
{
    [Fact]
    public async Task GetPendingAsync_Returns_Unsent_In_OrderAsync()
    {
        var store = new InMemoryOutboxStore();
        var now = DateTimeOffset.UtcNow;

        var first = new OutboxMessage(Ulid.NewUlid(), new byte[] { 1 }, new OutboxMessageOptions
        {
            EnqueuedTime = now.AddMinutes(-2)
        });
        var second = new OutboxMessage(Ulid.NewUlid(), new byte[] { 2 }, new OutboxMessageOptions
        {
            EnqueuedTime = now.AddMinutes(-1)
        });
        var sent = new OutboxMessage(Ulid.NewUlid(), new byte[] { 3 }, new OutboxMessageOptions
        {
            EnqueuedTime = now.AddMinutes(-3),
            SentTime = now.AddMinutes(-1)
        });

        await store.AddAsync(first);
        await store.AddAsync(second);
        await store.AddAsync(sent);

        var pending = await store.GetPendingAsync(10);

        Assert.Equal([first.MessageId, second.MessageId], pending.Select(message => message.MessageId));
    }
}
