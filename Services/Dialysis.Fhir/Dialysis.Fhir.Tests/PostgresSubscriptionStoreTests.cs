using BuildingBlocks.Tenancy;
using BuildingBlocks.Testcontainers;

using Dialysis.Fhir.Infrastructure.Persistence;
using Dialysis.Hl7ToFhir;

using Hl7.Fhir.Model;

using Microsoft.EntityFrameworkCore;

using Shouldly;

using Xunit;

namespace Dialysis.Fhir.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class PostgresSubscriptionStoreTests
{
    private readonly PostgreSqlFixture _fixture;

    public PostgresSubscriptionStoreTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async System.Threading.Tasks.Task Add_AndTryGet_ReturnsSubscriptionAsync()
    {
        await using FhirDbContext db = await CreateDbContextAsync();
        var tenant = new TenantContext { TenantId = "default" };
        var store = new PostgresSubscriptionStore(db, tenant);

        Subscription? sub = FhirJsonHelper.FromJson<Subscription>(
            """{"resourceType":"Subscription","status":"active","reason":"Test","criteria":"Observation","channel":{"type":"rest-hook","endpoint":"https://example.com/hook"}}""");
        sub = sub.ShouldNotBeNull();

        string id = "sub-" + Ulid.NewUlid();
        store.Add(id, sub);

        store.TryGet(id, out Subscription? retrieved).ShouldBeTrue();
        retrieved = retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(id);
        retrieved.Criteria.ShouldBe("Observation");
        retrieved.Channel?.Endpoint.ShouldBe("https://example.com/hook");
    }

    [Fact]
    public async System.Threading.Tasks.Task Remove_DeletesSubscriptionAsync()
    {
        await using FhirDbContext db = await CreateDbContextAsync();
        var tenant = new TenantContext { TenantId = "default" };
        var store = new PostgresSubscriptionStore(db, tenant);

        Subscription? sub = FhirJsonHelper.FromJson<Subscription>(
            """{"resourceType":"Subscription","status":"active","reason":"Test","criteria":"Procedure","channel":{"type":"rest-hook","endpoint":"https://example.com/proc"}}""");
        sub = sub.ShouldNotBeNull();

        string id = "sub-" + Ulid.NewUlid();
        store.Add(id, sub);
        store.TryGet(id, out _).ShouldBeTrue();

        bool removed = store.Remove(id);
        removed.ShouldBeTrue();
        store.TryGet(id, out _).ShouldBeFalse();
    }

    [Fact]
    public async System.Threading.Tasks.Task GetActiveRestHookSubscriptions_ReturnsOnlyActiveRestHooksAsync()
    {
        await using FhirDbContext db = await CreateDbContextAsync();
        var tenant = new TenantContext { TenantId = "default" };
        var store = new PostgresSubscriptionStore(db, tenant);

        Subscription? activeRestHook = FhirJsonHelper.FromJson<Subscription>(
            """{"resourceType":"Subscription","status":"active","reason":"Test","criteria":"Observation","channel":{"type":"rest-hook","endpoint":"https://example.com/obs"}}""");
        activeRestHook = activeRestHook.ShouldNotBeNull();
        store.Add("sub-active", activeRestHook);

        Subscription? offSub = FhirJsonHelper.FromJson<Subscription>(
            """{"resourceType":"Subscription","status":"off","reason":"Test","criteria":"Observation","channel":{"type":"rest-hook","endpoint":"https://example.com/off"}}""");
        offSub = offSub.ShouldNotBeNull();
        store.Add("sub-off", offSub);

        IReadOnlyList<Subscription> active = store.GetActiveRestHookSubscriptions();
        active.Count.ShouldBe(1);
        active[0].Id.ShouldBe("sub-active");
        active[0].Channel?.Endpoint.ShouldBe("https://example.com/obs");
    }

    [Fact]
    public async System.Threading.Tasks.Task TryGet_WithDifferentTenant_ReturnsFalseAsync()
    {
        await using FhirDbContext db = await CreateDbContextAsync();
        var tenant1 = new TenantContext { TenantId = "tenant-a" };
        var store1 = new PostgresSubscriptionStore(db, tenant1);

        Subscription? sub = FhirJsonHelper.FromJson<Subscription>(
            """{"resourceType":"Subscription","status":"active","reason":"Test","criteria":"Observation","channel":{"type":"rest-hook","endpoint":"https://example.com/hook"}}""");
        sub = sub.ShouldNotBeNull();

        string id = "sub-" + Ulid.NewUlid();
        store1.Add(id, sub);

        var tenant2 = new TenantContext { TenantId = "tenant-b" };
        var store2 = new PostgresSubscriptionStore(db, tenant2);
        store2.TryGet(id, out Subscription? _).ShouldBeFalse();
    }

    private async Task<FhirDbContext> CreateDbContextAsync()
    {
        DbContextOptions<FhirDbContext> options = new DbContextOptionsBuilder<FhirDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        var db = new FhirDbContext(options);
        _ = await db.Database.EnsureCreatedAsync();
        _ = await db.Subscriptions.ExecuteDeleteAsync();
        return db;
    }
}
