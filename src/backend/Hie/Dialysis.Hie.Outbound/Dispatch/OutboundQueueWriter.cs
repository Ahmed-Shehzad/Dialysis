using Dialysis.Hie.Core.Abstraction.Consent;
using Dialysis.Hie.Core.Abstraction.Mapping;
using Dialysis.Hie.Outbound.Domain;
using Dialysis.Hie.Outbound.Ports;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.Hie.Outbound.Dispatch;

/// <summary>
/// Helper invoked by event consumers. Maps the source event to a FHIR resource, checks consent, and
/// enqueues an <see cref="OutboundBundle"/> for the dispatcher to deliver.
/// </summary>
public sealed class OutboundQueueWriter(
    IOutboundBundleStore store,
    IConsentGate consentGate,
    TimeProvider timeProvider,
    IOptions<OutboundOptions> options,
    ILogger<OutboundQueueWriter> logger)
{
    private static readonly FhirJsonSerializer _serializer = new();
    private readonly OutboundOptions _options = options.Value;

    public async Task EnqueueAsync<TEvent, TResource>(
        TEvent integrationEvent,
        Guid patientId,
        IFhirMapper<TEvent, TResource> mapper,
        string consentScope,
        CancellationToken cancellationToken = default)
        where TEvent : class
        where TResource : Resource
    {
        var partnerId = _options.DefaultPartnerId;
        var allowed = await consentGate.CheckOutboundAsync(patientId, partnerId, consentScope, cancellationToken).ConfigureAwait(false);
        if (!allowed)
        {
            logger.LogInformation(
                "Outbound disclosure suppressed: no active consent for patient {PatientId} → partner {PartnerId} scope {Scope}",
                patientId, partnerId, consentScope);
            return;
        }

        var resource = mapper.Map(integrationEvent);
        var fhirJson = await _serializer.SerializeToStringAsync(resource).ConfigureAwait(false);

        var bundle = new OutboundBundle(
            patientId,
            resource.TypeName,
            resource.Id ?? Guid.NewGuid().ToString(),
            partnerId,
            fhirJson,
            timeProvider.GetUtcNow().UtcDateTime);

        await store.AddAsync(bundle, cancellationToken).ConfigureAwait(false);
        await store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
