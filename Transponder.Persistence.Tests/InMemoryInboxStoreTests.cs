using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.Tests;

public sealed class InMemoryInboxStoreTests
{
    [Fact]
    public async Task TryAddAsync_And_MarkProcessedAsync_Update_StateAsync()
    {
        var store = new InMemoryInboxStore();
        var messageId = Ulid.NewUlid();
        var state = new InboxState(messageId, "consumer-A");

        bool added = await store.TryAddAsync(state);

        DateTimeOffset processedTime = DateTimeOffset.UtcNow;
        await store.MarkProcessedAsync(messageId, "consumer-A", processedTime);
        IInboxState? loaded = await store.GetAsync(messageId, "consumer-A");

        Assert.True(added);
        Assert.NotNull(loaded);
        Assert.Equal(processedTime, loaded!.ProcessedTime);
    }
}
