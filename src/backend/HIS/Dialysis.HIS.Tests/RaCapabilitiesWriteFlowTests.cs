using Dialysis.CQRS;
using Dialysis.HIS.RaCapabilities.Features;
using Dialysis.HIS.RaCapabilities.Features.RecordMedicationDispensing;
using Dialysis.HIS.RaCapabilities.Features.RegisterFinancialErpLink;
using Dialysis.HIS.RaCapabilities.Ports;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

[Collection(nameof(HisFixtureCollection))]
public sealed class RaCapabilitiesWriteFlowTests
{
    private readonly HisApiWebApplicationFactory _factory;
    public RaCapabilitiesWriteFlowTests(HisApiWebApplicationFactory factory) => _factory = factory;
    [Fact]
    public async Task Registerfinancialerplink_Persists_And_Is_Listed_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var id = await gateway.SendCommandAsync<RegisterFinancialErpLinkCommand, Guid>(
            new RegisterFinancialErpLinkCommand(SystemCode: "ERP-TEST-A", StatusCode: "Active"),
            CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);

        var rows = await gateway.SendQueryAsync<ListFinancialErpLinksQuery, IReadOnlyList<RaFinancialErpLinkRow>>(
            new ListFinancialErpLinksQuery(),
            CancellationToken.None);

        rows.ShouldContain(r => r.Id == id && r.SystemCode == "ERP-TEST-A" && r.StatusCode == "Active");
    }

    [Fact]
    public async Task Registerfinancialerplink_Rejects_Invalid_Status_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        await Should.ThrowAsync<Exception>(async () =>
            await gateway.SendCommandAsync<RegisterFinancialErpLinkCommand, Guid>(
                new RegisterFinancialErpLinkCommand(SystemCode: "ERP-INVALID", StatusCode: "BogusStatus"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Recordmedicationdispensing_Persists_And_Is_Listed_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var orderId = Guid.CreateVersion7();
        var id = await gateway.SendCommandAsync<RecordMedicationDispensingCommand, Guid>(
            new RecordMedicationDispensingCommand(MedicationOrderId: orderId, BarcodeToken: "ABC123XYZ"),
            CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);

        var rows = await gateway.SendQueryAsync<ListMedicationDispensingRecordsQuery, IReadOnlyList<RaMedicationDispensingRow>>(
            new ListMedicationDispensingRecordsQuery(),
            CancellationToken.None);

        rows.ShouldContain(r => r.Id == id && r.MedicationOrderId == orderId && r.BarcodeToken == "ABC123XYZ");
    }

    [Fact]
    public async Task Recordmedicationdispensing_Rejects_Lowercase_Barcode_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        await Should.ThrowAsync<Exception>(async () =>
            await gateway.SendCommandAsync<RecordMedicationDispensingCommand, Guid>(
                new RecordMedicationDispensingCommand(MedicationOrderId: Guid.CreateVersion7(), BarcodeToken: "abc123"),
                CancellationToken.None));
    }
}
