using Dialysis.BuildingBlocks.DurableCommandBus;
using Dialysis.CQRS;
using Dialysis.HIS.Integration.DeviceIngestion;
using Dialysis.HIS.Integration.Features.IngestDeviceReading;
using Dialysis.HIS.PatientFlow.Features.AdmitPatient;
using Dialysis.HIS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Dialysis.HIS.Tests;

/// <summary>
/// End-to-end exercise of the durable-command path for HIS <c>IngestDeviceReading</c>.
/// Runs against the real Testcontainers Postgres + in-memory Transponder so the ledger
/// row + the aggregate change land in one transaction the way they will in production.
///
/// This is the contract lock-down for PR #141 — the synchronous path is covered by the
/// existing handler / repository tests; this file covers the durable rails specifically.
/// </summary>
[Collection(nameof(HisFixtureCollection))]
public sealed class IngestDeviceReadingDurablePathTests
{
    private readonly HisApiWebApplicationFactory _factory;
    /// <summary>
    /// End-to-end exercise of the durable-command path for HIS <c>IngestDeviceReading</c>.
    /// Runs against the real Testcontainers Postgres + in-memory Transponder so the ledger
    /// row + the aggregate change land in one transaction the way they will in production.
    ///
    /// This is the contract lock-down for PR #141 — the synchronous path is covered by the
    /// existing handler / repository tests; this file covers the durable rails specifically.
    /// </summary>
    public IngestDeviceReadingDurablePathTests(HisApiWebApplicationFactory factory) => _factory = factory;
    [Fact]
    public async Task Sync_Path_Generates_Server_Side_Id_When_Reading_Id_Is_Empty_Async()
    {
        // Synchronous fallback (the feature flag off path). Confirms the legacy behavior
        // still works after the [DurableCommand] + ReadingId additions in #141.
        using var scope = _factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();

        var patientId = Guid.CreateVersion7();
        var id = await gateway
            .SendCommandAsync<IngestDeviceReadingCommand, Guid>(
                new IngestDeviceReadingCommand("device-1", patientId, "{\"x\":1}"),
                CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        var record = await db.Set<DeviceReadingRecord>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, CancellationToken.None);
        record.ShouldNotBeNull();
        record.PatientId.ShouldBe(patientId);
        record.DeviceId.ShouldBe("device-1");
    }

    [Fact]
    public async Task Sync_Path_Uses_Explicit_Reading_Id_When_Supplied_Async()
    {
        // The id-from-CommandId trick: when the controller (durable path) calls the
        // handler with an explicit ReadingId, the handler uses it verbatim. This is what
        // makes a redelivery yield the same row + lets the 202 caller know the row id
        // without polling.
        using var scope = _factory.Services.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();
        var db = scope.ServiceProvider.GetRequiredService<HisDbContext>();

        var patientId = Guid.CreateVersion7();
        var explicitId = Guid.CreateVersion7();

        var returnedId = await gateway
            .SendCommandAsync<IngestDeviceReadingCommand, Guid>(
                new IngestDeviceReadingCommand("device-2", patientId, "{\"y\":2}", ReadingId: explicitId),
                CancellationToken.None);

        returnedId.ShouldBe(explicitId);
        var record = await db.Set<DeviceReadingRecord>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == explicitId, CancellationToken.None);
        record.ShouldNotBeNull();
    }

    [Fact]
    public async Task Durable_Bus_Enqueue_Returns_Acceptance_With_Stable_Command_Id_Async()
    {
        // The bus's contract — caller supplies a CommandId, the bus uses it as both the
        // ledger PK and the acceptance correlation marker. Same id → same row, idempotency
        // guaranteed by EfCommandLedger.TryClaimAsync.
        using var scope = _factory.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IDurableCommandBus>();

        var commandId = Guid.CreateVersion7();
        var patientId = Guid.CreateVersion7();
        var acceptance = await bus.EnqueueAsync<IngestDeviceReadingCommand, Guid>(
            new IngestDeviceReadingCommand("device-3", patientId, "{\"z\":3}", ReadingId: commandId),
            commandId: commandId,
            CancellationToken.None);

        acceptance.CommandId.ShouldBe(commandId);
        acceptance.CorrelationId.ShouldNotBeNullOrWhiteSpace();
        acceptance.StatusEndpoint.ShouldStartWith("/api/v1.0/command-status/");
    }

    [Fact]
    public async Task Durable_Bus_Rejects_Unregistered_Command_Type_Async()
    {
        // The catalog allowlist is the security gate — a command type that wasn't
        // registered in AddDurableCommandBus.RegisterCommand() must throw before any
        // publish happens. PR #140 ships RecordReadingCommand for PDMS;
        // PR #141 added IngestDeviceReadingCommand for HIS. AdmitPatientCommand is
        // intentionally NOT in either catalog, so it should be rejected here.
        using var scope = _factory.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IDurableCommandBus>();

        await Should.ThrowAsync<DurableCommandException>(() =>
            bus.EnqueueAsync<AdmitPatientCommand, Guid>(
                new AdmitPatientCommand(
                    Guid.CreateVersion7(), "WARD-A1"),
                commandId: Guid.CreateVersion7(),
                CancellationToken.None));
    }
}
