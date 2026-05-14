using Dialysis.CQRS;
using Dialysis.HIS.DataServices.Features.ManagerDashboard;
using Dialysis.HIS.DataServices.Ports;
using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Domain.ValueObjects;
using Dialysis.HIS.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

[Collection(nameof(HisFixtureCollection))]
public sealed class ManagerDashboardFlowTests(HisApiWebApplicationFactory factory)
{
    [Fact]
    public async Task Snapshot_counts_queued_billing_and_open_quality_tasks_and_echoes_focus()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var queued = BillingExportJob.Queue(
            new PayerCode("ACME-01"),
            new BillingPeriod(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)),
            notes: null,
            nowUtc: DateTime.UtcNow);
        db.BillingExportJobs.Add(queued);

        var completed = BillingExportJob.Queue(
            new PayerCode("ACME-02"),
            new BillingPeriod(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30)),
            notes: null,
            nowUtc: DateTime.UtcNow.AddDays(-3));
        completed.MarkCompleted(DateTime.UtcNow.AddDays(-2));
        db.BillingExportJobs.Add(completed);

        db.RaQualityWorkflowTasks.Add(new RaQualityWorkflowTask
        {
            Id = Guid.CreateVersion7(),
            TaskCode = "Q-1",
            Title = "Open audit",
            StatusCode = "open",
            OpenedAtUtc = DateTime.UtcNow,
        });
        db.RaQualityWorkflowTasks.Add(new RaQualityWorkflowTask
        {
            Id = Guid.CreateVersion7(),
            TaskCode = "Q-2",
            Title = "Closed audit",
            StatusCode = "closed",
            OpenedAtUtc = DateTime.UtcNow.AddDays(-1),
            ClosedAtUtc = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(CancellationToken.None);

        var snapshot = await gateway.SendQueryAsync<ManagerDashboardQuery, ManagerDashboardSnapshotDto>(
            new ManagerDashboardQuery(ReportFocus: "billing"),
            CancellationToken.None);

        snapshot.ReportFocus.ShouldBe("billing");
        snapshot.QueuedBillingExportJobsCount.ShouldBeGreaterThanOrEqualTo(1);
        snapshot.OpenQualityWorkflowTasksCount.ShouldBeGreaterThanOrEqualTo(1);
    }
}
