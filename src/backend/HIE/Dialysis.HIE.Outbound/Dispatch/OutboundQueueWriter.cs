using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.BuildingBlocks.Fhir.Tefca;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Outbound.Partners;
using Dialysis.HIE.Outbound.Ports;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Outbound.Dispatch;

/// <summary>
/// Helper invoked by event consumers. Maps the source event to a FHIR resource, checks consent, and
/// enqueues an <see cref="OutboundBundle"/> for the dispatcher to deliver.
/// </summary>
public sealed class OutboundQueueWriter
{
    private readonly IOutboundBundleStore _store;
    private readonly IConsentGate _consentGate;
    private readonly IPartnerRouter _partnerRouter;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboundQueueWriter> _logger;
    /// <summary>
    /// Helper invoked by event consumers. Maps the source event to a FHIR resource, checks consent, and
    /// enqueues an <see cref="OutboundBundle"/> for the dispatcher to deliver.
    /// </summary>
    public OutboundQueueWriter(IOutboundBundleStore store,
        IConsentGate consentGate,
        IPartnerRouter partnerRouter,
        TimeProvider timeProvider,
        ILogger<OutboundQueueWriter> logger)
    {
        _store = store;
        _consentGate = consentGate;
        _partnerRouter = partnerRouter;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // ToJson is CPU-only; calling it from a non-Async method keeps VSTHRD103 quiet.
    private static string SerializeFhirJson(Resource resource) => resource.ToJson();

    public async Task EnqueueAsync<TEvent, TResource>(
        TEvent integrationEvent,
        Guid patientId,
        IFhirResourceMapper<TEvent, TResource> mapper,
        string consentScope,
        string? purposeOfUse = null,
        CancellationToken cancellationToken = default)
        where TEvent : class
        where TResource : Resource
    {
        // Cross-org clinical disclosures are made for care delivery unless a consumer says otherwise.
        var purpose = string.IsNullOrWhiteSpace(purposeOfUse) ? TefcaPermittedPurposes.Treatment : purposeOfUse;
        var partners = _partnerRouter.ResolvePartners(patientId, consentScope);

        var resource = mapper.Map(integrationEvent);
        var fhirJson = SerializeFhirJson(resource);
        var logicalId = resource.Id ?? Guid.NewGuid().ToString();
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var enqueuedAny = false;
        foreach (var partnerId in partners)
        {
            var allowed = await _consentGate.CheckOutboundAsync(patientId, partnerId, consentScope, purpose, cancellationToken).ConfigureAwait(false);
            if (!allowed)
            {
                _logger.LogInformation(
                    "Outbound disclosure suppressed: no active consent for patient {PatientId} → partner {PartnerId} scope {Scope} purpose {Purpose}",
                    patientId, partnerId, consentScope, purpose);
                continue;
            }

            var bundle = new OutboundBundle(patientId, resource.TypeName, logicalId, partnerId, fhirJson, now, purpose);
            await _store.AddAsync(bundle, cancellationToken).ConfigureAwait(false);
            enqueuedAny = true;
        }

        if (enqueuedAny)
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
