namespace Transponder.Persistence.Tests;

public sealed class InMemoryInboxStoreTests
{
    [Fact]
    public async Task TryAddAsync_And_MarkProcessedAsync_Update_StateAsync()
    {
        var store = new InMemoryInboxStore();
        var messageId = Ulid.NewUlid();
        var state = new InboxState(messageId, "consumer-A");

        var added = await store.TryAddAsync(state);

        var processedTime = DateTimeOffset.UtcNow;
        await store.MarkProcessedAsync(messageId, "consumer-A", processedTime);
        var loaded = await store.GetAsync(messageId, "consumer-A");

        Assert.True(added);
        Assert.NotNull(loaded);
        Assert.Equal(processedTime, loaded!.ProcessedTime);
    }
}
