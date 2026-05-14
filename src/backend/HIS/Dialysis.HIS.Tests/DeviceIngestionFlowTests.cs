using Dialysis.CQRS;
using Dialysis.HIS.Integration.Features.IngestDeviceReading;
using Dialysis.HIS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

[Collection(nameof(HisFixtureCollection))]
public sealed class DeviceIngestionFlowTests(HisApiWebApplicationFactory factory)
{
    [Fact]
    public async Task Ingesting_a_reading_persists_to_the_facility_database()
    {
        using var scope = factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();

        var cmd = new IngestDeviceReadingCommand(
            DeviceId: "machine-42",
            PatientId: Guid.CreateVersion7(),
            PayloadJson: "{\"flow_rate_ml_min\":350}",
            ExternalMessageId: "msg-1");

        var id = await gateway
            .SendCommandAsync<IngestDeviceReadingCommand, Guid>(cmd, CancellationToken.None) ;

        var row = await db.DeviceReadings
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, CancellationToken.None) ;

        row.ShouldNotBeNull();
        row.DeviceId.ShouldBe("machine-42");
        row.ExternalMessageId.ShouldBe("msg-1");
    }

    [Fact]
    public async Task Reingesting_the_same_external_message_id_returns_the_same_row()
    {
        using var scope = factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var cmd = new IngestDeviceReadingCommand(
            DeviceId: "machine-77",
            PatientId: Guid.CreateVersion7(),
            PayloadJson: "{}",
            ExternalMessageId: "dedupe-key-1");

        var first = await gateway
            .SendCommandAsync<IngestDeviceReadingCommand, Guid>(cmd, CancellationToken.None) ;
        var second = await gateway
            .SendCommandAsync<IngestDeviceReadingCommand, Guid>(cmd, CancellationToken.None) ;

        second.ShouldBe(first);
    }
}
