using Microsoft.EntityFrameworkCore;

namespace Transponder.Persistence.EntityFramework.Tests;

public sealed class EntityFrameworkInboxStoreTests
{
    [Fact]
    public async Task TryAddAsync_Returns_False_For_Duplicate_StateAsync()
    {
        await using EntityFrameworkTestDbContext context = CreateContext(nameof(TryAddAsync_Returns_False_For_Duplicate_StateAsync));
        var store = new EntityFrameworkInboxStore(context);
        var state = new InboxState(Ulid.NewUlid(), "consumer-A");

        bool firstAdd = await store.TryAddAsync(state);
        _ = await context.SaveChangesAsync();
        bool secondAdd = await store.TryAddAsync(state);

        Assert.True(firstAdd);
        Assert.False(secondAdd);
    }

    private static EntityFrameworkTestDbContext CreateContext(string databaseName)
    {
        DbContextOptions<EntityFrameworkTestDbContext> options = new DbContextOptionsBuilder<EntityFrameworkTestDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new EntityFrameworkTestDbContext(options);
    }
}
