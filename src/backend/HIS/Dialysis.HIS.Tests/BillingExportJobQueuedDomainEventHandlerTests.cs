using Dialysis.CQRS;
using Dialysis.HIS.Operations.Features.SubmitBillingExportJob;
using Dialysis.HIS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

[Collection(nameof(HisFixtureCollection))]
public sealed class BillingExportJobQueuedDomainEventHandlerTests
{
    private readonly HisApiWebApplicationFactory _factory;
    public BillingExportJobQueuedDomainEventHandlerTests(HisApiWebApplicationFactory factory) => _factory = factory;
    [Fact]
    public async Task Submitbillingexportjob_Records_Audit_Via_Domain_Event_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();

        var payer = $"AUD-{Guid.NewGuid().ToString("N").ToUpperInvariant()[..8]}";
        var id = await gateway.SendCommandAsync<SubmitBillingExportJobCommand, Guid>(
            new SubmitBillingExportJobCommand(
                PayerCode: payer,
                PeriodStart: new DateOnly(2026, 8, 1),
                PeriodEnd: new DateOnly(2026, 8, 31)),
            CancellationToken.None);

        var audit = await db.BillingExportJobAudits
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.JobId == id, CancellationToken.None);

        audit.ShouldNotBeNull();
        audit.PayerCode.ShouldBe(payer);
        audit.PeriodStart.ShouldBe(new DateOnly(2026, 8, 1));
    }
}
