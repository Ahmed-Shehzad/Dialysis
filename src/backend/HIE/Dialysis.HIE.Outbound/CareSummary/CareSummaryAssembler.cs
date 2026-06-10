using System.Text;
using Dialysis.BuildingBlocks.Fhir.CdaBridge;
using Dialysis.BuildingBlocks.Fhir.Tefca;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Outbound.Partners;
using Dialysis.HIE.Outbound.Ports;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.HIE.Outbound.CareSummary;

/// <summary>
/// Outcome of a care-summary assembly: whether a CCD was produced and queued, the queued
/// bundle's id, how many source resources it summarised, and (when not generated) why.
/// </summary>
public sealed record CareSummaryResult(bool Generated, Guid? OutboundBundleId, int ResourceCount, string? Reason)
{
    public static CareSummaryResult NotGenerated(string reason) => new(false, null, 0, reason);
}

/// <summary>
/// Assembles a Continuity of Care Document (C-CDA R2.1 CCD) for one patient and queues it for
/// Directed Exchange. Directed Exchange in the real world is "send a clinical summary", not "send
/// one Observation" — so this gathers the FHIR resources HIE has already mapped for the patient
/// (its <see cref="OutboundBundle"/> rows), folds them into a single FHIR document Bundle, runs the
/// <see cref="IFhirToCdaMapper"/> to emit the CCD, wraps it in a FHIR <c>DocumentReference</c>
/// (CCD bytes as the attachment), and enqueues that as a normal outbound bundle — so the existing
/// <c>OutboundDispatcher</c> + partner endpoint deliver it unchanged.
/// </summary>
public sealed class CareSummaryAssembler
{
    /// <summary>LOINC for "Summary of episode note" — the CCD document type code.</summary>
    public const string CcdLoinc = "34133-9";

    /// <summary>Media type the CCD attachment is tagged with on the DocumentReference.</summary>
    public const string CcdContentType = "application/cda+xml";

    private static readonly FhirJsonDeserializer _parser =
        new(new DeserializerSettings().UsingMode(DeserializationMode.Recoverable));

    private readonly IOutboundBundleStore _store;
    private readonly IFhirToCdaMapper _cdaMapper;
    private readonly IConsentGate _consentGate;
    private readonly IPartnerRouter _partnerRouter;
    private readonly TimeProvider _timeProvider;
    private readonly OutboundOptions _options;
    private readonly ILogger<CareSummaryAssembler> _logger;

    public CareSummaryAssembler(
        IOutboundBundleStore store,
        IFhirToCdaMapper cdaMapper,
        IConsentGate consentGate,
        IPartnerRouter partnerRouter,
        TimeProvider timeProvider,
        IOptions<OutboundOptions> options,
        ILogger<CareSummaryAssembler> logger)
    {
        _store = store;
        _cdaMapper = cdaMapper;
        _consentGate = consentGate;
        _partnerRouter = partnerRouter;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    // ToJson is CPU-only; isolating it keeps VSTHRD103 quiet.
    private static string SerializeFhirJson(Resource resource) => resource.ToJson();

    /// <summary>
    /// Builds a CCD for <paramref name="patientId"/> and enqueues it for dispatch. Returns a
    /// result describing what happened (no resources → nothing queued; consent denied → suppressed).
    /// </summary>
    public async Task<CareSummaryResult> AssembleAndEnqueueAsync(
        Guid patientId,
        string? purposeOfUse = null,
        string? destinationPartnerId = null,
        CancellationToken cancellationToken = default)
    {
        var purpose = string.IsNullOrWhiteSpace(purposeOfUse) ? TefcaPermittedPurposes.Treatment : purposeOfUse;
        // A referral targets an explicit destination; otherwise route to the primary partner.
        var partnerId = !string.IsNullOrWhiteSpace(destinationPartnerId)
            ? destinationPartnerId
            : _partnerRouter.ResolvePartners(patientId, ConsentScopes.ClinicalNotes).FirstOrDefault() ?? _options.DefaultPartnerId;

        var bundles = await _store.ListForPatientAsync(patientId, cancellationToken).ConfigureAwait(false);
        var resources = ParseLatestPerResource(bundles);
        if (resources.Count == 0)
        {
            return CareSummaryResult.NotGenerated("No mapped resources for the patient to summarise.");
        }

        // The CCD is a clinical document; gate it under the document/clinical-note consent scope.
        var allowed = await _consentGate
            .CheckOutboundAsync(patientId, partnerId, ConsentScopes.ClinicalNotes, purpose, cancellationToken)
            .ConfigureAwait(false);
        if (!allowed)
        {
            _logger.LogInformation(
                "Care summary suppressed: no active consent for patient {PatientId} → partner {PartnerId} purpose {Purpose}",
                patientId, partnerId, purpose);
            return CareSummaryResult.NotGenerated("No active consent for a care-summary disclosure.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var documentBundle = new Bundle
        {
            Type = Bundle.BundleType.Document,
            Timestamp = now,
            Entry = resources
                .Select(r => new Bundle.EntryComponent { Resource = r })
                .ToList(),
        };

        var ccdXml = _cdaMapper.Map(documentBundle);

        var documentReference = BuildDocumentReference(patientId, ccdXml, now);
        var outbound = new OutboundBundle(
            patientId,
            nameof(DocumentReference),
            documentReference.Id!,
            partnerId,
            SerializeFhirJson(documentReference),
            now,
            purpose);

        await _store.AddAsync(outbound, cancellationToken).ConfigureAwait(false);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Care summary CCD queued for patient {PatientId} ({ResourceCount} resources) as bundle {BundleId}",
            patientId, resources.Count, outbound.Id);
        return new CareSummaryResult(true, outbound.Id, resources.Count, null);
    }

    // Keep the freshest resource per (type, logical id) so a CCD reflects current state, not a
    // replay of every historical event (no event sourcing — this is a projection over current rows).
    private List<Resource> ParseLatestPerResource(IReadOnlyList<OutboundBundle> bundles)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var resources = new List<Resource>();
        // bundles already arrive most-recent first.
        foreach (var bundle in bundles)
        {
            var key = $"{bundle.ResourceType}/{bundle.LogicalId}";
            if (!seen.Add(key)) continue;
            try
            {
                resources.Add(_parser.Deserialize<Resource>(bundle.FhirJson));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping unparseable outbound bundle {BundleId} during care-summary assembly", bundle.Id);
            }
        }

        // Patient first so the CCD recordTarget resolves; the emitter takes the first Patient it sees.
        resources.Sort((a, b) => Rank(a).CompareTo(Rank(b)));
        return resources;
    }

    private static int Rank(Resource resource) => resource is Patient ? 0 : 1;

    private static DocumentReference BuildDocumentReference(Guid patientId, string ccdXml, DateTime now)
    {
        var bytes = Encoding.UTF8.GetBytes(ccdXml);
        return new DocumentReference
        {
            Id = Guid.NewGuid().ToString(),
            Status = DocumentReferenceStatus.Current,
            Type = new CodeableConcept("http://loinc.org", CcdLoinc, "Summary of episode note"),
            Subject = new ResourceReference($"Patient/{patientId}"),
            Date = new DateTimeOffset(DateTime.SpecifyKind(now, DateTimeKind.Utc)),
            Content =
            [
                new DocumentReference.ContentComponent
                {
                    Attachment = new Attachment
                    {
                        ContentType = CcdContentType,
                        Data = bytes,
                        Title = "Continuity of Care Document",
                        Creation = new DateTimeOffset(DateTime.SpecifyKind(now, DateTimeKind.Utc)).ToString("o"),
                    },
                },
            ],
        };
    }
}
