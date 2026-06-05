using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Persistence;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Outbound;

public sealed class ReferralRequestedConsumerTests
{
    [Fact]
    public async Task Referral_Pushes_A_Ccd_To_The_Destination_Partner_Async()
    {
        const string target = "referral-target";
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var patientId = Guid.NewGuid();
        var db = sp.GetRequiredService<HieDbContext>();

        // Consent to disclose to the referral destination + a resource to summarise.
        db.Consents.Add(new ConsentRecord(
            patientId, target, ConsentScopes.ClinicalNotes, ConsentDirection.Outbound,
            DateTime.UtcNow.AddMinutes(-1), effectiveToUtc: null, purpose: null));
        db.OutboundBundles.Add(new OutboundBundle(
            patientId, "Patient", patientId.ToString(), target,
            new Patient { Id = patientId.ToString() }.ToJson(), DateTime.UtcNow));
        await db.SaveChangesAsync();

        var consumer = sp.GetServices<IConsumer<ReferralRequestedIntegrationEvent>>().Single();
        await consumer.HandleAsync(new ConsumeContext<ReferralRequestedIntegrationEvent>(
            new ReferralRequestedIntegrationEvent(
                EventId: Guid.NewGuid(),
                OccurredOn: DateTime.UtcNow,
                SchemaVersion: 1,
                PatientId: patientId,
                DestinationPartnerId: target,
                ReferringProviderId: Guid.NewGuid(),
                ReferralReason: "transfer of care",
                RequestedAtUtc: DateTime.UtcNow),
            CancellationToken.None,
            new NoopBus()));

        var ccd = db.OutboundBundles.Single(b => b.ResourceType == nameof(DocumentReference));
        ccd.PartnerId.ShouldBe(target);
        ccd.Status.ShouldBe(OutboundBundleStatus.Pending);
    }
}
