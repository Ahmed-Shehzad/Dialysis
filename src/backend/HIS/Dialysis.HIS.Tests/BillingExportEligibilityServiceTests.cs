using Dialysis.CQRS;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.HIS.Operations.Features.SubmitBillingExportJob;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

[Collection(nameof(HisFixtureCollection))]
public sealed class BillingExportEligibilityServiceTests
{
    private readonly HisApiWebApplicationFactory _factory;
    public BillingExportEligibilityServiceTests(HisApiWebApplicationFactory factory) => _factory = factory;
    [Fact]
    public async Task Submitting_A_Second_Queued_Export_For_Same_Payer_And_Period_Throws_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var payer = $"PYR-{Guid.NewGuid().ToString("N").ToUpperInvariant()[..8]}";
        var cmd = new SubmitBillingExportJobCommand(
            PayerCode: payer,
            PeriodStart: new DateOnly(2026, 7, 1),
            PeriodEnd: new DateOnly(2026, 7, 31));

        await gateway.SendCommandAsync<SubmitBillingExportJobCommand, Guid>(cmd, CancellationToken.None);

        var ex = await Should.ThrowAsync<DomainException>(async () =>
            await gateway.SendCommandAsync<SubmitBillingExportJobCommand, Guid>(cmd, CancellationToken.None));

        ex.Message.ShouldContain("queued export already exists");
    }
}
