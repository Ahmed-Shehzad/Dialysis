using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.HIE.Core.Abstraction.Partners;
using Dialysis.HIE.Outbound.Consumers;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Persistence;
using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Tests.Outbound;

public sealed class OutboundFlowTests
{
    [Fact]
    public async Task Patientregistered_Enqueues_Pending_Bundle_Then_Dispatcher_Marks_Delivered_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        // Seed an active outbound consent for the patient.
        var patientId = Guid.NewGuid();
        var db = sp.GetRequiredService<HieDbContext>();
        db.Consents.Add(new ConsentRecord(
            patientId,
            "default",
            ConsentScopes.Demographics,
            ConsentDirection.Outbound,
            DateTime.UtcNow.AddMinutes(-1),
            effectiveToUtc: null));
        await db.SaveChangesAsync();

        // Dispatch the integration event through the in-process consumer.
        var consumer = sp.GetServices<IConsumer<PatientRegisteredIntegrationEvent>>().Single();
        var evt = new PatientRegisteredIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            PatientId: patientId,
            MedicalRecordNumber: "MRN-123",
            FamilyName: "Doe",
            GivenName: "Jane",
            DateOfBirth: new DateOnly(1980, 1, 15),
            SexAtBirthCode: "Female",
            PreferredLanguageCode: "en");
        await consumer.HandleAsync(new ConsumeContext<PatientRegisteredIntegrationEvent>(
            evt,
            CancellationToken.None,
            new NoopBus()));

        var pending = db.OutboundBundles.Single();
        pending.Status.ShouldBe(OutboundBundleStatus.Pending);
        pending.ResourceType.ShouldBe("Patient");
        pending.PatientId.ShouldBe(patientId);

        // Tick the dispatcher → ACK partner delivers → status flips to Delivered.
        var dispatcher = sp.GetRequiredService<IOutboundDispatcher>();
        var processed = await dispatcher.TickAsync();
        processed.ShouldBe(1);

        var delivered = db.OutboundBundles.Single();
        delivered.Status.ShouldBe(OutboundBundleStatus.Delivered);
        delivered.DeliveredAtUtc.ShouldNotBeNull();
    }
}

internal sealed class NoopBus : ITransponderBus
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
    public Task PublishAsync<TMessage>(TMessage message, TransponderPublishOptions options, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
    public Task PublishPreparedAsync(string routingKey, object message, TransponderPublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PublishLargeAsync<TMessage>(TMessage message, TransponderLargeMessageOptions? options = null, CancellationToken cancellationToken = default) where TMessage : class => Task.CompletedTask;
}

internal sealed class StubPartnerEndpoint : IPartnerEndpoint
{
    public string PartnerId => "default";
    public List<Resource> Delivered { get; } = [];

    public Task<PartnerDeliveryResult> DeliverAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        Delivered.Add(resource);
        return Task.FromResult(new PartnerDeliveryResult(true, 200, null));
    }
}
