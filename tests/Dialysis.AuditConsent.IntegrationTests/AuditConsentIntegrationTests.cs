using System.Linq;
using Dialysis.AuditConsent.Data;
using Dialysis.AuditConsent.Features.Audit;
using Dialysis.IntegrationFixtures;
using Dialysis.Tenancy;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Dialysis.AuditConsent.IntegrationTests;

[Collection("Postgres")]
public sealed class AuditConsentIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public AuditConsentIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(RecordAuditHandler RecordHandler, GetAuditQueryHandler QueryHandler)> CreateHandlersAsync()
    {
        var conn = _fixture.GetConnectionStringForDatabase("dialysis_audit_default");
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.MigrationsAssembly("Dialysis.AuditConsent"))
            .Options;

        await using (var db = new AuditDbContext(options))
        {
            await db.Database.MigrateAsync();
            await db.AuditEvents.ExecuteDeleteAsync();
        }

        var connectionResolver = Substitute.For<ITenantConnectionResolver>();
        connectionResolver.GetConnectionString(Arg.Any<string>()).Returns(conn);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns("default");

        var dbFactory = new TenantAuditDbContextFactory(tenantContext, connectionResolver);

        return (new RecordAuditHandler(dbFactory, tenantContext), new GetAuditQueryHandler(dbFactory));
    }

    [Fact]
    public async Task RecordAuditHandler_persists_audit_event()
    {
        var (recordHandler, _) = await CreateHandlersAsync();
        var cmd = new RecordAuditCommand
        {
            ResourceType = "Observation",
            ResourceId = "obs-123",
            Action = "create",
            AgentId = "system",
            Outcome = "0"
        };

        await recordHandler.HandleAsync(cmd);

        var conn = _fixture.GetConnectionStringForDatabase("dialysis_audit_default");
        await using var db = new AuditDbContext(new DbContextOptionsBuilder<AuditDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.MigrationsAssembly("Dialysis.AuditConsent")).Options);
        var events = await db.AuditEvents.Where(e => e.ResourceId == "obs-123").ToListAsync();
        events.Count.ShouldBe(1);
        events[0].ResourceType.ShouldBe("Observation");
        events[0].Action.ShouldBe("create");
        events[0].TenantId.ShouldBe("default");
    }

    [Fact]
    public async Task GetAuditQueryHandler_returns_audit_entries_after_record()
    {
        var (recordHandler, queryHandler) = await CreateHandlersAsync();
        await recordHandler.HandleAsync(new RecordAuditCommand
        {
            ResourceType = "Patient",
            ResourceId = "p-1",
            Action = "create",
            AgentId = "user1",
            Outcome = "0"
        });

        var result = await queryHandler.HandleAsync(new GetAuditQuery());

        result.Entries.Count.ShouldBeGreaterThanOrEqualTo(1);
        var patientEntry = result.Entries.FirstOrDefault(e => e.ResourceId == "p-1");
        patientEntry.ShouldNotBeNull();
        patientEntry!.ResourceType.ShouldBe("Patient");
        patientEntry.Action.ShouldBe("create");
    }

    [Fact]
    public async Task GetAuditQueryHandler_filters_by_resource_type()
    {
        var (recordHandler, queryHandler) = await CreateHandlersAsync();
        await recordHandler.HandleAsync(new RecordAuditCommand { ResourceType = "Observation", ResourceId = "o1", Action = "create", AgentId = "sys", Outcome = "0" });
        await recordHandler.HandleAsync(new RecordAuditCommand { ResourceType = "Patient", ResourceId = "p1", Action = "create", AgentId = "sys", Outcome = "0" });

        var result = await queryHandler.HandleAsync(new GetAuditQuery { ResourceType = "Observation" });

        result.Entries.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.Entries.ShouldAllBe(e => e.ResourceType == "Observation");
    }

    [Fact]
    public async Task GetAuditQueryHandler_filters_by_resource_id()
    {
        var (recordHandler, queryHandler) = await CreateHandlersAsync();
        var resourceId = "unique-res-" + Guid.NewGuid();
        await recordHandler.HandleAsync(new RecordAuditCommand { ResourceType = "Encounter", ResourceId = resourceId, Action = "update", AgentId = "sys", Outcome = "0" });

        var result = await queryHandler.HandleAsync(new GetAuditQuery { ResourceId = resourceId });

        result.Entries.Count.ShouldBe(1);
        result.Entries[0].ResourceId.ShouldBe(resourceId);
        result.Entries[0].Action.ShouldBe("update");
    }

    [Fact]
    public async Task GetAuditQueryHandler_empty_when_no_events()
    {
        var (_, queryHandler) = await CreateHandlersAsync();
        var result = await queryHandler.HandleAsync(new GetAuditQuery());
        result.Entries.Count.ShouldBe(0);
        result.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task RecordAuditHandler_multiple_events_for_same_resource()
    {
        var (recordHandler, queryHandler) = await CreateHandlersAsync();
        var resourceId = "multi-res-" + Guid.NewGuid();

        await recordHandler.HandleAsync(new RecordAuditCommand { ResourceType = "Observation", ResourceId = resourceId, Action = "create", AgentId = "sys", Outcome = "0" });
        await recordHandler.HandleAsync(new RecordAuditCommand { ResourceType = "Observation", ResourceId = resourceId, Action = "update", AgentId = "sys", Outcome = "0" });
        await recordHandler.HandleAsync(new RecordAuditCommand { ResourceType = "Observation", ResourceId = resourceId, Action = "read", AgentId = "sys", Outcome = "0" });

        var result = await queryHandler.HandleAsync(new GetAuditQuery { ResourceId = resourceId });
        result.Entries.Count.ShouldBe(3);
        result.Entries.Select(e => e.Action).ShouldContain("create");
        result.Entries.Select(e => e.Action).ShouldContain("update");
        result.Entries.Select(e => e.Action).ShouldContain("read");
    }
}
