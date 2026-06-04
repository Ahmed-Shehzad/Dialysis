using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Outbound.Ports;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Outbound.Dispatch;

/// <summary>
/// Helper invoked by event consumers. Maps the source event to a FHIR resource, checks consent, and
/// enqueues an <see cref="OutboundBundle"/> for the dispatcher to deliver.
/// </summary>
public sealed class OutboundQueueWriter
{
    private readonly OutboundOptions _options;
    private readonly IOutboundBundleStore _store;
    private readonly IConsentGate _consentGate;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboundQueueWriter> _logger;
    /// <summary>
    /// Helper invoked by event consumers. Maps the source event to a FHIR resource, checks consent, and
    /// enqueues an <see cref="OutboundBundle"/> for the dispatcher to deliver.
    /// </summary>
    public OutboundQueueWriter(IOutboundBundleStore store,
        IConsentGate consentGate,
        TimeProvider timeProvider,
        IOptions<OutboundOptions> options,
        ILogger<OutboundQueueWriter> logger)
    {
        _store = store;
        _consentGate = consentGate;
        _timeProvider = timeProvider;
        _logger = logger;
        _options = options.Value;
    }

    // ToJson is CPU-only; calling it from a non-Async method keeps VSTHRD103 quiet.
    private static string SerializeFhirJson(Resource resource) => resource.ToJson();

    public async Task EnqueueAsync<TEvent, TResource>(
        TEvent integrationEvent,
        Guid patientId,
        IFhirResourceMapper<TEvent, TResource> mapper,
        string consentScope,
        CancellationToken cancellationToken = default)
        where TEvent : class
        where TResource : Resource
    {
        var partnerId = _options.DefaultPartnerId;
        var allowed = await _consentGate.CheckOutboundAsync(patientId, partnerId, consentScope, cancellationToken).ConfigureAwait(false);
        if (!allowed)
        {
            _logger.LogInformation(
                "Outbound disclosure suppressed: no active consent for patient {PatientId} → partner {PartnerId} scope {Scope}",
                patientId, partnerId, consentScope);
            return;
        }

        var resource = mapper.Map(integrationEvent);
        var fhirJson = SerializeFhirJson(resource);

        var bundle = new OutboundBundle(
            patientId,
            resource.TypeName,
            resource.Id ?? Guid.NewGuid().ToString(),
            partnerId,
            fhirJson,
            _timeProvider.GetUtcNow().UtcDateTime);

        await _store.AddAsync(bundle, cancellationToken).ConfigureAwait(false);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
