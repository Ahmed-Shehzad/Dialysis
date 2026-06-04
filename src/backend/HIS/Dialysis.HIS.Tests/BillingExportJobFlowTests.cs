using Dialysis.CQRS;
using Dialysis.HIS.Operations.Features.GetBillingExportJobById;
using Dialysis.HIS.Operations.Features.SubmitBillingExportJob;
using Dialysis.HIS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

[Collection(nameof(HisFixtureCollection))]
public sealed class BillingExportJobFlowTests
{
    private readonly HisApiWebApplicationFactory _factory;
    public BillingExportJobFlowTests(HisApiWebApplicationFactory factory) => _factory = factory;
    [Fact]
    public async Task Submitting_A_Job_Persists_Status_Queued_And_Enqueues_Outbox_Event_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();

        var cmd = new SubmitBillingExportJobCommand(
            PayerCode: "AETNA",
            PeriodStart: new DateOnly(2026, 5, 1),
            PeriodEnd: new DateOnly(2026, 5, 31),
            Notes: "Test export window");

        var id = await gateway.SendCommandAsync<SubmitBillingExportJobCommand, Guid>(cmd, CancellationToken.None);

        var dto = await gateway.SendQueryAsync<GetBillingExportJobByIdQuery, BillingExportJobStatusDto?>(
            new GetBillingExportJobByIdQuery(id),
            CancellationToken.None);

        dto.ShouldNotBeNull();
        dto.PayerCode.ShouldBe("AETNA");
        dto.StatusCode.ShouldBe("Queued");
        dto.Notes.ShouldBe("Test export window");

        var outboxRow = await db.OutboxMessages
            .AsNoTracking()
            .Where(x => x.AssemblyQualifiedEventType.Contains("BillingExportJobQueuedIntegrationEvent"))
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(CancellationToken.None);

        outboxRow.ShouldNotBeNull();
        outboxRow.PayloadJson.ShouldContain("AETNA");
        outboxRow.PayloadJson.ShouldContain(id.ToString());
    }

    [Fact]
    public async Task Getting_A_Missing_Job_Returns_Null_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var dto = await gateway.SendQueryAsync<GetBillingExportJobByIdQuery, BillingExportJobStatusDto?>(
            new GetBillingExportJobByIdQuery(Guid.CreateVersion7()),
            CancellationToken.None);

        dto.ShouldBeNull();
    }
}
