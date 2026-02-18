using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.Tests;

public sealed class InMemoryScheduledMessageStoreTests
{
    [Fact]
    public async Task GetDueAsync_Returns_Due_Messages_In_OrderAsync()
    {
        var store = new InMemoryScheduledMessageStore();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var dueFirst = new ScheduledMessage(Ulid.NewUlid(), "Test", new byte[] { 1 }, new ScheduledMessageTimestamps(now.AddMinutes(-2)));
        var dueSecond = new ScheduledMessage(Ulid.NewUlid(), "Test", new byte[] { 2 }, new ScheduledMessageTimestamps(now.AddMinutes(-1)));
        var future = new ScheduledMessage(Ulid.NewUlid(), "Test", new byte[] { 3 }, new ScheduledMessageTimestamps(now.AddMinutes(5)));

        await store.AddAsync(dueSecond);
        await store.AddAsync(future);
        await store.AddAsync(dueFirst);

        IReadOnlyList<IScheduledMessage> due = await store.GetDueAsync(now, 10);

        Assert.Equal([dueFirst.TokenId, dueSecond.TokenId], due.Select(message => message.TokenId));
    }
}
