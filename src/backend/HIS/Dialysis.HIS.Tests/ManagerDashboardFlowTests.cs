using Dialysis.CQRS;
using Dialysis.HIS.DataServices.Features.ManagerDashboard;
using Dialysis.HIS.DataServices.Ports;
using Dialysis.HIS.Operations.Domain;
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

        db.BillingExportJobs.Add(new BillingExportJob
        {
            Id = Guid.CreateVersion7(),
            PayerCode = "ACME-01",
            StatusCode = "Queued",
            PeriodStart = new DateOnly(2026, 5, 1),
            PeriodEnd = new DateOnly(2026, 5, 31),
            SubmittedAtUtc = DateTime.UtcNow,
        });
        db.BillingExportJobs.Add(new BillingExportJob
        {
            Id = Guid.CreateVersion7(),
            PayerCode = "ACME-02",
            StatusCode = "Completed",
            PeriodStart = new DateOnly(2026, 4, 1),
            PeriodEnd = new DateOnly(2026, 4, 30),
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-3),
            CompletedAtUtc = DateTime.UtcNow.AddDays(-2),
        });

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
