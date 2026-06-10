using Dialysis.CQRS;
using Dialysis.HIS.Integration.Features.IngestDeviceReading;
using Dialysis.HIS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

[Collection(nameof(HisFixtureCollection))]
public sealed class DeviceIngestionFlowTests
{
    private readonly HisApiWebApplicationFactory _factory;
    public DeviceIngestionFlowTests(HisApiWebApplicationFactory factory) => _factory = factory;
    [Fact]
    public async Task Ingesting_A_Reading_Persists_To_The_Facility_Database_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();

        var cmd = new IngestDeviceReadingCommand(
            DeviceId: "machine-42",
            PatientId: Guid.CreateVersion7(),
            PayloadJson: "{\"flow_rate_ml_min\":350}",
            ExternalMessageId: "msg-1");

        var id = await gateway
            .SendCommandAsync<IngestDeviceReadingCommand, Guid>(cmd, CancellationToken.None);

        var row = await db.DeviceReadings
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, CancellationToken.None);

        row.ShouldNotBeNull();
        row.DeviceId.ShouldBe("machine-42");
        row.ExternalMessageId.ShouldBe("msg-1");
    }

    [Fact]
    public async Task Reingesting_The_Same_External_Message_Id_Returns_The_Same_Row_Async()
    {
        using var scope = _factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var cmd = new IngestDeviceReadingCommand(
            DeviceId: "machine-77",
            PatientId: Guid.CreateVersion7(),
            PayloadJson: "{}",
            ExternalMessageId: "dedupe-key-1");

        var first = await gateway
            .SendCommandAsync<IngestDeviceReadingCommand, Guid>(cmd, CancellationToken.None);
        var second = await gateway
            .SendCommandAsync<IngestDeviceReadingCommand, Guid>(cmd, CancellationToken.None);

        second.ShouldBe(first);
    }
}
