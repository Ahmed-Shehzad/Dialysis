using Microsoft.EntityFrameworkCore;

using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.EntityFramework.Tests;

public sealed class EntityFrameworkOutboxStoreTests
{
    [Fact]
    public async Task GetPendingAsync_Returns_Unsent_MessagesAsync()
    {
        await using EntityFrameworkTestDbContext context = CreateContext(nameof(GetPendingAsync_Returns_Unsent_MessagesAsync));
        var store = new EntityFrameworkOutboxStore(context);

        var pending = new OutboxMessage(Ulid.NewUlid(), new byte[] { 1 });
        var sent = new OutboxMessage(Ulid.NewUlid(), new byte[] { 2 }, new OutboxMessageOptions
        {
            SentTime = DateTimeOffset.UtcNow
        });

        await store.AddAsync(pending);
        await store.AddAsync(sent);
        _ = await context.SaveChangesAsync();

        IReadOnlyList<IOutboxMessage> results = await store.GetPendingAsync(10);

        _ = Assert.Single(results);
        Assert.Equal(pending.MessageId, results[0].MessageId);
    }

    private static EntityFrameworkTestDbContext CreateContext(string databaseName)
    {
        DbContextOptions<EntityFrameworkTestDbContext> options = new DbContextOptionsBuilder<EntityFrameworkTestDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new EntityFrameworkTestDbContext(options);
    }
}
