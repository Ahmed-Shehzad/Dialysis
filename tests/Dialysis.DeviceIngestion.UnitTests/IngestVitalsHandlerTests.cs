using Bogus;
using Dialysis.DeviceIngestion.Features.IngestVitals;
using Dialysis.DeviceIngestion.Services;
using Dialysis.Tenancy;
using Dialysis.TestUtilities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Dialysis.DeviceIngestion.UnitTests;

public sealed class IngestVitalsHandlerTests
{
    [Fact]
    public async Task HandleAsync_writes_observations_and_returns_ids()
    {
        var writer = Substitute.For<IFhirObservationWriter>();
        writer.WriteObservationsAsync(Arg.Any<string?>(), "p1", "e1", "dev1", Arg.Any<IReadOnlyList<VitalReading>>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "obs-1", "obs-2" });

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns("default");

        var handler = new IngestVitalsHandler(writer, tenantContext);
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate() with { PatientId = "p1", EncounterId = "e1", DeviceId = "dev1" };

        var result = await handler.HandleAsync(cmd);

        result.ObservationIds.ShouldBe(["obs-1", "obs-2"]);
        await writer.Received(1).WriteObservationsAsync("default", "p1", "e1", "dev1", Arg.Is<IReadOnlyList<VitalReading>>(r => r.Count == cmd.Readings.Count), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_passes_tenant_id()
    {
        var writer = Substitute.For<IFhirObservationWriter>();
        writer.WriteObservationsAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<IReadOnlyList<VitalReading>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns("tenant-x");

        var handler = new IngestVitalsHandler(writer, tenantContext);
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate();

        await handler.HandleAsync(cmd);

        await writer.Received(1).WriteObservationsAsync("tenant-x", cmd.PatientId, cmd.EncounterId, cmd.DeviceId, Arg.Any<IReadOnlyList<VitalReading>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_null_tenant_uses_context_value()
    {
        var writer = Substitute.For<IFhirObservationWriter>();
        writer.WriteObservationsAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<IReadOnlyList<VitalReading>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns((string?)null);

        var handler = new IngestVitalsHandler(writer, tenantContext);
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate();

        await handler.HandleAsync(cmd);

        await writer.Received(1).WriteObservationsAsync(Arg.Is<string?>(x => x == null), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<IReadOnlyList<VitalReading>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_empty_deviceId_passed_to_writer()
    {
        var writer = Substitute.For<IFhirObservationWriter>();
        writer.WriteObservationsAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<IReadOnlyList<VitalReading>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns("default");

        var handler = new IngestVitalsHandler(writer, tenantContext);
        var cmd = BogusFakers.IngestVitalsCommandFaker().Generate() with { DeviceId = "" };

        await handler.HandleAsync(cmd);

        await writer.Received(1).WriteObservationsAsync(Arg.Any<string?>(), cmd.PatientId, cmd.EncounterId, "", Arg.Any<IReadOnlyList<VitalReading>>(), Arg.Any<CancellationToken>());
    }
}
