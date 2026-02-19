using BuildingBlocks.Testcontainers;

using Microsoft.EntityFrameworkCore;

namespace Transponder.Persistence.EntityFramework.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class EntityFrameworkInboxStoreTests
{
    private readonly PostgreSqlFixture _fixture;

    public EntityFrameworkInboxStoreTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task TryAddAsync_Returns_False_For_Duplicate_StateAsync()
    {
        await using EntityFrameworkTestDbContext context = CreateContext();
        _ = await context.Database.EnsureCreatedAsync();
        var store = new EntityFrameworkInboxStore(context);
        var state = new InboxState(Ulid.NewUlid(), "consumer-A");

        bool firstAdd = await store.TryAddAsync(state);
        _ = await context.SaveChangesAsync();
        bool secondAdd = await store.TryAddAsync(state);

        Assert.True(firstAdd);
        Assert.False(secondAdd);
    }

    private EntityFrameworkTestDbContext CreateContext()
    {
        DbContextOptions<EntityFrameworkTestDbContext> options = new DbContextOptionsBuilder<EntityFrameworkTestDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new EntityFrameworkTestDbContext(options);
    }
}
