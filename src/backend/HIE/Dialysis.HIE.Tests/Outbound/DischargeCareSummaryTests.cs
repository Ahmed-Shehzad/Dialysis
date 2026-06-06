using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.HIE.Outbound;
using Dialysis.HIE.Outbound.CareSummary;
using Dialysis.HIE.Outbound.Consumers;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Outbound.Mappers;
using Dialysis.HIE.Outbound.Partners;
using Dialysis.HIE.Persistence;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Outbound;

public sealed class DischargeCareSummaryTests
{
    [Fact]
    public async Task Discharge_Pushes_A_Ccd_When_Enabled_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var (sp, db, patientId) = await Seed_Async(scope.ServiceProvider);

        var consumer = MakeConsumer(sp, autoDischarge: true);
        await consumer.HandleAsync(EncounterClosed(patientId));

        db.OutboundBundles.Any(b => b.ResourceType == nameof(DocumentReference) && b.PartnerId == "default")
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Discharge_Pushes_No_Ccd_When_Disabled_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var (sp, db, patientId) = await Seed_Async(scope.ServiceProvider);

        var consumer = MakeConsumer(sp, autoDischarge: false);
        await consumer.HandleAsync(EncounterClosed(patientId));

        db.OutboundBundles.Any(b => b.ResourceType == nameof(DocumentReference)).ShouldBeFalse();
    }

    private static async Task<(IServiceProvider Sp, HieDbContext Db, Guid PatientId)> Seed_Async(IServiceProvider sp)
    {
        var patientId = Guid.NewGuid();
        var db = sp.GetRequiredService<HieDbContext>();
        db.Consents.Add(new ConsentRecord(
            patientId, "default", ConsentScopes.ClinicalNotes, ConsentDirection.Outbound,
            DateTime.UtcNow.AddMinutes(-1), effectiveToUtc: null, purpose: null));
        db.OutboundBundles.Add(new OutboundBundle(
            patientId, "Patient", patientId.ToString(), "default",
            new Patient { Id = patientId.ToString() }.ToJson(), DateTime.UtcNow));
        await db.SaveChangesAsync();
        return (sp, db, patientId);
    }

    private static EncounterClosedConsumer MakeConsumer(IServiceProvider sp, bool autoDischarge) => new(
        sp.GetRequiredService<OutboundQueueWriter>(),
        sp.GetRequiredService<EncounterMapper>(),
        sp.GetRequiredService<CareSummaryAssembler>(),
        sp.GetRequiredService<IPartnerRouter>(),
        Options.Create(new OutboundOptions { AutoDischargeSummary = autoDischarge, DefaultPartnerId = "default" }));

    private static ConsumeContext<EncounterClosedIntegrationEvent> EncounterClosed(Guid patientId) =>
        new(
            new EncounterClosedIntegrationEvent(
                EventId: Guid.NewGuid(),
                OccurredOn: DateTime.UtcNow,
                SchemaVersion: 1,
                EncounterId: Guid.NewGuid(),
                PatientId: patientId,
                ProviderId: Guid.NewGuid(),
                ClosedAtUtc: DateTime.UtcNow,
                DiagnosisIcd10Codes: ["E11.9"],
                ProcedureCptCodes: []),
            CancellationToken.None,
            new NoopBus());
}
