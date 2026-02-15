using System.Linq;
using Dialysis.Alerting.Data;
using Dialysis.Alerting.Features.ProcessAlerts;
using Dialysis.Alerting.Services;
using Dialysis.Tenancy;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Dialysis.Tests;

[Collection("Postgres")]
public sealed class AlertingIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public AlertingIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(CreateAlertHandler CreateHandler, AcknowledgeAlertHandler AckHandler, ListAlertsQueryHandler ListHandler)> CreateHandlersAsync()
    {
        var conn = _fixture.GetConnectionStringForDatabase("dialysis_alerting_default");
        var options = new DbContextOptionsBuilder<AlertDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.MigrationsAssembly("Dialysis.Alerting"))
            .Options;

        await using (var db = new AlertDbContext(options))
        {
            await db.Database.MigrateAsync();
            await db.Alerts.ExecuteDeleteAsync();
        }

        var connectionResolver = Substitute.For<ITenantConnectionResolver>();
        connectionResolver.GetConnectionString(Arg.Any<string>()).Returns(conn);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns("default");

        var dbFactory = new TenantAlertDbContextFactory(tenantContext, connectionResolver);
        var cache = Substitute.For<IAlertCacheService>();
        cache.GetListAsync(Arg.Any<AlertStatusFilter?>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyList<AlertSummaryDto>?)null);

        return (
            new CreateAlertHandler(dbFactory, cache),
            new AcknowledgeAlertHandler(dbFactory, cache),
            new ListAlertsQueryHandler(dbFactory, cache)
        );
    }

    [Fact]
    public async Task CreateAlertHandler_persists_alert()
    {
        var (createHandler, _, _) = await CreateHandlersAsync();
        var cmd = new CreateAlertCommand
        {
            PatientId = "p1",
            EncounterId = "e1",
            Code = "HYPOTENSION_RISK",
            Severity = "high",
            Message = "Systolic BP critically low"
        };

        var result = await createHandler.HandleAsync(cmd);

        result.AlertId.ShouldNotBeNullOrEmpty();
        var conn = _fixture.GetConnectionStringForDatabase("dialysis_alerting_default");
        await using var db = new AlertDbContext(new DbContextOptionsBuilder<AlertDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.MigrationsAssembly("Dialysis.Alerting")).Options);
        var alert = await db.Alerts.FindAsync(result.AlertId);
        alert.ShouldNotBeNull();
        alert!.PatientId.ShouldBe("p1");
        alert.Code.ShouldBe("HYPOTENSION_RISK");
        alert.Severity.ShouldBe("high");
    }

    [Fact]
    public async Task AcknowledgeAlertHandler_updates_status_to_acknowledged()
    {
        var (createHandler, ackHandler, _) = await CreateHandlersAsync();
        var createResult = await createHandler.HandleAsync(new CreateAlertCommand
        {
            PatientId = "p1",
            EncounterId = "e1",
            Code = "HYPOTENSION_RISK",
            Severity = "high",
            Message = "Test"
        });

        await ackHandler.HandleAsync(new AcknowledgeAlertCommand { AlertId = createResult.AlertId });

        var conn = _fixture.GetConnectionStringForDatabase("dialysis_alerting_default");
        await using var db = new AlertDbContext(new DbContextOptionsBuilder<AlertDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.MigrationsAssembly("Dialysis.Alerting")).Options);
        var alert = await db.Alerts.FindAsync(createResult.AlertId);
        alert.ShouldNotBeNull();
        alert!.Status.ShouldBe(AlertStatus.Acknowledged);
        alert.AcknowledgedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task AcknowledgeAlertHandler_nonexistent_does_not_throw()
    {
        var (_, ackHandler, _) = await CreateHandlersAsync();
        await ackHandler.HandleAsync(new AcknowledgeAlertCommand { AlertId = "nonexistent-" + Guid.NewGuid() });
    }

    [Fact]
    public async Task ListAlertsQueryHandler_returns_created_alerts()
    {
        var (createHandler, _, listHandler) = await CreateHandlersAsync();
        await createHandler.HandleAsync(new CreateAlertCommand { PatientId = "pa", EncounterId = "ea", Code = "CODE1", Severity = "high", Message = "M1" });
        await createHandler.HandleAsync(new CreateAlertCommand { PatientId = "pb", EncounterId = "eb", Code = "CODE2", Severity = "low", Message = "M2" });

        var result = await listHandler.HandleAsync(new ListAlertsQuery { Status = null });

        result.Alerts.Count.ShouldBeGreaterThanOrEqualTo(2);
        result.Alerts.Select(a => a.Code).ShouldContain("CODE1");
        result.Alerts.Select(a => a.Code).ShouldContain("CODE2");
    }

    [Fact]
    public async Task ListAlertsQueryHandler_filters_by_active()
    {
        var (createHandler, ackHandler, listHandler) = await CreateHandlersAsync();
        var r1 = await createHandler.HandleAsync(new CreateAlertCommand { PatientId = "p1", EncounterId = "e1", Code = "A1", Severity = "high", Message = "M1" });
        var r2 = await createHandler.HandleAsync(new CreateAlertCommand { PatientId = "p2", EncounterId = "e2", Code = "A2", Severity = "high", Message = "M2" });
        await ackHandler.HandleAsync(new AcknowledgeAlertCommand { AlertId = r1.AlertId });

        var result = await listHandler.HandleAsync(new ListAlertsQuery { Status = AlertStatusFilter.Active });

        result.Alerts.All(a => a.Status == AlertStatus.Active.ToString()).ShouldBeTrue();
        result.Alerts.Select(a => a.Id).ShouldNotContain(r1.AlertId);
        result.Alerts.Select(a => a.Id).ShouldContain(r2.AlertId);
    }

    [Fact]
    public async Task ListAlertsQueryHandler_filters_by_acknowledged()
    {
        var (createHandler, ackHandler, listHandler) = await CreateHandlersAsync();
        var r1 = await createHandler.HandleAsync(new CreateAlertCommand { PatientId = "p1", EncounterId = "e1", Code = "A1", Severity = "high", Message = "M1" });
        await ackHandler.HandleAsync(new AcknowledgeAlertCommand { AlertId = r1.AlertId });

        var result = await listHandler.HandleAsync(new ListAlertsQuery { Status = AlertStatusFilter.Acknowledged });

        result.Alerts.Count.ShouldBeGreaterThanOrEqualTo(1);
        result.Alerts.All(a => a.Status == AlertStatus.Acknowledged.ToString()).ShouldBeTrue();
    }

    [Fact]
    public async Task ListAlertsQueryHandler_empty_when_no_alerts()
    {
        var (_, _, listHandler) = await CreateHandlersAsync();
        var result = await listHandler.HandleAsync(new ListAlertsQuery());
        result.Alerts.Count.ShouldBe(0);
        result.Alerts.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAlert_with_message_persists_message()
    {
        var (createHandler, _, _) = await CreateHandlersAsync();
        var msg = "Critical hypotension detected - BP 82/45";
        var result = await createHandler.HandleAsync(new CreateAlertCommand
        {
            PatientId = "p1",
            EncounterId = "e1",
            Code = "HYPOTENSION",
            Severity = "critical",
            Message = msg
        });

        var conn = _fixture.GetConnectionStringForDatabase("dialysis_alerting_default");
        await using var db = new AlertDbContext(new DbContextOptionsBuilder<AlertDbContext>()
            .UseNpgsql(conn, npgsql => npgsql.MigrationsAssembly("Dialysis.Alerting")).Options);
        var alert = await db.Alerts.FindAsync(result.AlertId);
        alert!.Message.ShouldBe(msg);
    }
}
