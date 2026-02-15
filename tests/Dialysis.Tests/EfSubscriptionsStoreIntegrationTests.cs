using FhirCore.Subscriptions;
using FhirCore.Subscriptions.Data;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.Tests;

[Collection("Postgres")]
public sealed class EfSubscriptionsStoreIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public EfSubscriptionsStoreIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<ISubscriptionsStore> CreateStoreAsync()
    {
        var conn = _fixture.GetConnectionStringForDatabase("fhir_subscriptions");
        var services = new ServiceCollection();
        services.AddDbContextFactory<SubscriptionDbContext>(options =>
            options.UseNpgsql(conn));
        services.AddSingleton<ISubscriptionsStore, EfSubscriptionsStore>();
        var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SubscriptionDbContext>>();
            await using var db = await factory.CreateDbContextAsync();
            await db.Database.MigrateAsync();
            await db.Subscriptions.ExecuteDeleteAsync(); // Isolate tests
        }

        return provider.GetRequiredService<ISubscriptionsStore>();
    }

    [Fact]
    public async Task AddAsync_and_GetByIdAsync_roundtrip()
    {
        var store = await CreateStoreAsync();
        var id = Ulid.NewUlid().ToString();
        var entry = new SubscriptionEntry
        {
            Id = id,
            Criteria = "Observation",
            Endpoint = "https://webhook.example.com/obs",
            EndpointType = "webhook"
        };

        await store.AddAsync(entry);

        var retrieved = await store.GetByIdAsync(id);
        retrieved.ShouldNotBeNull();
        retrieved!.Id.ShouldBe(id);
        retrieved.Criteria.ShouldBe("Observation");
        retrieved.Endpoint.ShouldBe("https://webhook.example.com/obs");
    }

    [Fact]
    public async Task GetAllAsync_returns_active_subscriptions()
    {
        var store = await CreateStoreAsync();
        var id1 = Ulid.NewUlid().ToString();
        var id2 = Ulid.NewUlid().ToString();

        await store.AddAsync(new SubscriptionEntry { Id = id1, Criteria = "Patient", Endpoint = "https://a.com", EndpointType = "webhook" });
        await store.AddAsync(new SubscriptionEntry { Id = id2, Criteria = "Encounter", Endpoint = "https://b.com", EndpointType = "webhook" });

        var all = await store.GetAllAsync();
        all.Count.ShouldBe(2);
        all.Select(s => s.Id).ShouldContain(id1);
        all.Select(s => s.Id).ShouldContain(id2);
    }

    [Fact]
    public async Task UpdateAsync_modifies_subscription()
    {
        var store = await CreateStoreAsync();
        var id = Ulid.NewUlid().ToString();
        await store.AddAsync(new SubscriptionEntry { Id = id, Criteria = "Observation", Endpoint = "https://old.com", EndpointType = "webhook" });

        var updated = await store.UpdateAsync(id, new SubscriptionEntry
        {
            Id = id,
            Criteria = "Observation?patient=123",
            Endpoint = "https://new.com",
            EndpointType = "webhook"
        });

        updated.ShouldBeTrue();
        var retrieved = await store.GetByIdAsync(id);
        retrieved!.Endpoint.ShouldBe("https://new.com");
        retrieved.Criteria.ShouldBe("Observation?patient=123");
    }

    [Fact]
    public async Task RemoveAsync_deletes_subscription()
    {
        var store = await CreateStoreAsync();
        var id = Ulid.NewUlid().ToString();
        await store.AddAsync(new SubscriptionEntry { Id = id, Criteria = "Patient", Endpoint = "https://x.com", EndpointType = "webhook" });

        var removed = await store.RemoveAsync(id);

        removed.ShouldBeTrue();
        var retrieved = await store.GetByIdAsync(id);
        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task AddAsync_duplicate_id_throws()
    {
        var store = await CreateStoreAsync();
        var id = Ulid.NewUlid().ToString();
        var entry = new SubscriptionEntry { Id = id, Criteria = "X", Endpoint = "https://x.com", EndpointType = "webhook" };

        await store.AddAsync(entry);

        var ex = await Should.ThrowAsync<InvalidOperationException>(async () => await store.AddAsync(entry));
        ex.Message.ShouldContain(id);
    }

    [Fact]
    public async Task GetByIdAsync_nonexistent_returns_null()
    {
        var store = await CreateStoreAsync();
        var retrieved = await store.GetByIdAsync("nonexistent-id-" + Ulid.NewUlid());
        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveAsync_nonexistent_returns_false()
    {
        var store = await CreateStoreAsync();
        var removed = await store.RemoveAsync("nonexistent-id-" + Ulid.NewUlid());
        removed.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateAsync_nonexistent_returns_false()
    {
        var store = await CreateStoreAsync();
        var id = "nonexistent-" + Ulid.NewUlid();
        var updated = await store.UpdateAsync(id, new SubscriptionEntry
        {
            Id = id,
            Criteria = "Patient",
            Endpoint = "https://x.com",
            EndpointType = "webhook"
        });
        updated.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAllAsync_returns_empty_when_no_subscriptions()
    {
        var store = await CreateStoreAsync();
        var all = await store.GetAllAsync();
        all.Count.ShouldBe(0);
        all.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddAsync_with_query_params_in_criteria()
    {
        var store = await CreateStoreAsync();
        var id = Ulid.NewUlid().ToString();
        var entry = new SubscriptionEntry
        {
            Id = id,
            Criteria = "Observation?patient=Patient/123&encounter=enc-1",
            Endpoint = "https://webhook.example.com",
            EndpointType = "webhook"
        };

        await store.AddAsync(entry);
        var retrieved = await store.GetByIdAsync(id);
        retrieved.ShouldNotBeNull();
        retrieved!.Criteria.ShouldContain("patient=Patient/123");
        retrieved.Criteria.ShouldContain("encounter=enc-1");
    }

    [Fact]
    public async Task UpdateAsync_id_mismatch_still_updates_by_provided_id()
    {
        var store = await CreateStoreAsync();
        var id = Ulid.NewUlid().ToString();
        await store.AddAsync(new SubscriptionEntry { Id = id, Criteria = "Patient", Endpoint = "https://old.com", EndpointType = "webhook" });

        var updated = await store.UpdateAsync(id, new SubscriptionEntry
        {
            Id = id,
            Criteria = "Patient?active=true",
            Endpoint = "https://updated.com",
            EndpointType = "webhook"
        });

        updated.ShouldBeTrue();
        var retrieved = await store.GetByIdAsync(id);
        retrieved!.Criteria.ShouldBe("Patient?active=true");
        retrieved.Endpoint.ShouldBe("https://updated.com");
    }
}
