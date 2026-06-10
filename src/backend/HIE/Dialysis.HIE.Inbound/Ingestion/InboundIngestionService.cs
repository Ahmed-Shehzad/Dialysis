using System.Globalization;
using System.Text.Json;
using Dialysis.BuildingBlocks.Fhir.Tefca;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.HIE.Contracts.Integration;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.HIE.Core.Coding;
using Dialysis.HIE.Inbound.Domain;
using Dialysis.HIE.Inbound.Mpi;
using Dialysis.HIE.Inbound.Ports;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Inbound.Ingestion;

/// <summary>
/// Validates, consent-gates, persists, and republishes inbound FHIR resources. Returns an
/// <see cref="OperationOutcome"/> describing the result for the FHIR controller to serialise back.
/// </summary>
public sealed class InboundIngestionService
{
    private readonly IReceivedResourceStore _resourceStore;

    private readonly IPatientIndex _patientIndex;

    private readonly PatientMatchService _matchService;

    private readonly IPatientLinkReviewStore _linkReviews;

    private readonly IConsentGate _consentGate;

    private readonly ITransponderOutbox? _transponderOutbox;

    private readonly TimeProvider _timeProvider;

    private readonly MpiMatchOptions _matchOptions;

    private readonly ILogger<InboundIngestionService> _logger;
    /// <summary>
    /// Validates, consent-gates, persists, and republishes inbound FHIR resources. Returns an
    /// <see cref="OperationOutcome"/> describing the result for the FHIR controller to serialise back.
    /// </summary>
    public InboundIngestionService(IReceivedResourceStore resourceStore,
        IPatientIndex patientIndex,
        PatientMatchService matchService,
        IPatientLinkReviewStore linkReviews,
        IConsentGate consentGate,
        ITransponderOutbox? transponderOutbox,
        TimeProvider timeProvider,
        IOptions<MpiMatchOptions> matchOptions,
        ILogger<InboundIngestionService> logger)
    {
        _resourceStore = resourceStore;
        _patientIndex = patientIndex;
        _matchService = matchService;
        _linkReviews = linkReviews;
        _consentGate = consentGate;
        _transponderOutbox = transponderOutbox;
        _timeProvider = timeProvider;
        _matchOptions = matchOptions?.Value ?? new MpiMatchOptions();
        _logger = logger;
    }
    // ToJson is CPU-only; calling it from a non-Async method keeps VSTHRD103 quiet.
    private static string SerializeFhirJson(Resource resource) => resource.ToJson();

    public async Task<OperationOutcome> IngestAsync(string partnerId, Resource resource, string? purposeOfUse = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partnerId);
        ArgumentNullException.ThrowIfNull(resource);

        // Inbound writes are consumed for care delivery unless the partner asserts another purpose.
        var purpose = string.IsNullOrWhiteSpace(purposeOfUse) ? TefcaPermittedPurposes.Treatment : purposeOfUse;

        var outcome = new OperationOutcome();
        if (resource is Bundle bundle)
        {
            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is null) continue;
                await IngestSingleAsync(partnerId, entry.Resource, purpose, outcome, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            await IngestSingleAsync(partnerId, resource, purpose, outcome, cancellationToken).ConfigureAwait(false);
        }

        await _resourceStore.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (outcome.Issue.Count == 0)
            outcome.Issue.Add(InformationIssue("All entries accepted."));
        return outcome;
    }

    private async Task IngestSingleAsync(string partnerId, Resource resource, string purpose, OperationOutcome outcome, CancellationToken cancellationToken)
    {
        var logicalId = resource.Id ?? Guid.NewGuid().ToString();
        var scope = ScopeFor(resource);

        var consented = await _consentGate.CheckInboundAsync(logicalId, partnerId, scope, purpose, cancellationToken).ConfigureAwait(false);
        if (!consented)
        {
            outcome.Issue.Add(new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Warning,
                Code = OperationOutcome.IssueType.Suppressed,
                Diagnostics = $"No active inbound consent for {resource.TypeName}/{logicalId} from partner {partnerId}.",
            });
            return;
        }

        var fhirJson = SerializeFhirJson(resource);
        var received = new ReceivedResource(
            partnerId,
            resource.TypeName,
            logicalId,
            fhirJson,
            _timeProvider.GetUtcNow().UtcDateTime,
            validationOutcome: "accepted");
        await _resourceStore.UpsertAsync(received, cancellationToken).ConfigureAwait(false);

        await ProjectAsync(partnerId, resource, logicalId, cancellationToken).ConfigureAwait(false);
        outcome.Issue.Add(new OperationOutcome.IssueComponent
        {
            Severity = OperationOutcome.IssueSeverity.Information,
            Code = OperationOutcome.IssueType.Informational,
            Diagnostics = $"Accepted {resource.TypeName}/{logicalId}.",
        });
    }

    private async Task ProjectAsync(string partnerId, Resource resource, string logicalId, CancellationToken cancellationToken)
    {
        switch (resource)
        {
            case Patient patient:
                await ProjectPatientAsync(partnerId, patient, logicalId, cancellationToken).ConfigureAwait(false);
                break;
            case Encounter encounter:
                await EmitAsync(new ExternalEncounterIngestedIntegrationEvent(
                    Guid.NewGuid(),
                    _timeProvider.GetUtcNow().UtcDateTime,
                    SchemaVersion: 1,
                    partnerId,
                    logicalId,
                    encounter.Subject?.Reference,
                    (encounter.Period?.StartElement is { } pStart && DateTime.TryParse(pStart.Value, out var s)) ? s : null,
                    (encounter.Period?.EndElement is { } pEnd && DateTime.TryParse(pEnd.Value, out var en)) ? en : null,
                    encounter.Class?.Code,
                    encounter.ReasonCode.FirstOrDefault()?.Coding.FirstOrDefault()?.Code), cancellationToken).ConfigureAwait(false);
                break;
            case Observation observation:
                {
                    var loinc = observation.Code?.Coding.FirstOrDefault()?.Code ?? "unknown";
                    var display = observation.Code?.Coding.FirstOrDefault()?.Display ?? observation.Code?.Text ?? string.Empty;
                    string? valueQuantity = null;
                    string? unit = null;
                    if (observation.Value is Quantity q)
                    {
                        valueQuantity = q.Value?.ToString(CultureInfo.InvariantCulture);
                        unit = q.Unit;
                    }
                    else if (observation.Value is FhirString fs)
                    {
                        valueQuantity = fs.Value;
                    }
                    var observed = observation.Effective is FhirDateTime fdt && DateTime.TryParse(fdt.Value, out var ts) ? ts : (DateTime?)null;
                    await EmitAsync(new ExternalLabResultIngestedIntegrationEvent(
                        Guid.NewGuid(),
                        _timeProvider.GetUtcNow().UtcDateTime,
                        SchemaVersion: 1,
                        partnerId,
                        logicalId,
                        observation.Subject?.Reference,
                        loinc,
                        display,
                        valueQuantity,
                        unit,
                        observed), cancellationToken).ConfigureAwait(false);
                    break;
                }
            case Procedure procedure:
                {
                    DateTime? start = null, end = null;
                    if (procedure.Performed is Period p)
                    {
                        if (p.StartElement is { } ps && DateTime.TryParse(ps.Value, out var pSt)) start = pSt;
                        if (p.EndElement is { } pe && DateTime.TryParse(pe.Value, out var pEn)) end = pEn;
                    }
                    await EmitAsync(new ExternalDialysisSessionIngestedIntegrationEvent(
                        Guid.NewGuid(),
                        _timeProvider.GetUtcNow().UtcDateTime,
                        SchemaVersion: 1,
                        partnerId,
                        logicalId,
                        procedure.Subject?.Reference,
                        start,
                        end,
                        procedure.Outcome?.Coding.FirstOrDefault()?.Code), cancellationToken).ConfigureAwait(false);
                    break;
                }
            default:
                _logger.LogDebug("No inbound projection implemented for {ResourceType}", resource.TypeName);
                break;
        }
    }

    private async Task ProjectPatientAsync(string partnerId, Patient patient, string logicalId, CancellationToken cancellationToken)
    {
        var name = patient.Name.FirstOrDefault();
        var family = name?.Family;
        var given = name?.Given?.FirstOrDefault();
        DateOnly? dob = null;
        if (!string.IsNullOrWhiteSpace(patient.BirthDate) && DateOnly.TryParse(patient.BirthDate, out var parsedDob))
            dob = parsedDob;
        var mrn = patient.Identifier
            .Find(i => string.Equals(i.System, CodeSystems.MrnIdentifier, StringComparison.Ordinal))?.Value;

        var entry = new PatientIndexEntry(
            partnerId,
            logicalId,
            mrn,
            family,
            given,
            dob,
            patient.Gender?.ToString(),
            _timeProvider.GetUtcNow().UtcDateTime);
        var persisted = await _patientIndex.UpsertAsync(entry, cancellationToken).ConfigureAwait(false);
        await DetectDuplicatesAsync(persisted, cancellationToken).ConfigureAwait(false);

        await EmitAsync(new ExternalPatientReferenceIngestedIntegrationEvent(
            Guid.NewGuid(),
            _timeProvider.GetUtcNow().UtcDateTime,
            SchemaVersion: 1,
            partnerId,
            logicalId,
            mrn,
            family,
            given,
            dob,
            patient.Gender?.ToString()), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Probabilistic duplicate detection: scores the just-ingested patient against existing index
    /// entries. A cross-source <see cref="Mpi.MatchGrade.Certain"/> match is auto-linked when the site
    /// opts in (<see cref="Mpi.MpiMatchOptions.AutoLinkCertainMatches"/>); everything else that scores
    /// Probable/Certain is queued for a steward. Deduped on the unordered entry pair so repeated
    /// ingests don't pile up reviews.
    /// </summary>
    private async Task DetectDuplicatesAsync(PatientIndexEntry source, CancellationToken cancellationToken)
    {
        var criteria = new PatientMatchCriteria(
            source.MedicalRecordNumber, source.FamilyName, source.GivenName, source.DateOfBirth, source.SexAtBirthCode);

        var matches = await _matchService.FindMatchesAsync(criteria, take: 10, cancellationToken).ConfigureAwait(false);
        foreach (var match in matches)
        {
            if (match.Entry.Id == source.Id || match.Grade < MatchGrade.Probable)
            {
                continue;
            }
            if (await _linkReviews.ExistsForPairAsync(source.Id, match.Entry.Id, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var crossSource = !string.Equals(source.PartnerId, match.Entry.PartnerId, StringComparison.Ordinal);

            if (_matchOptions.AutoLinkCertainMatches && match.Grade == MatchGrade.Certain && crossSource)
            {
                _linkReviews.Add(PatientLinkReview.AutoLink(
                    source.Id, source.PartnerId, Label(source),
                    match.Entry.Id, match.Entry.PartnerId, Label(match.Entry),
                    match.Score, match.Grade, _matchOptions.AutoLinkActor, now));
                _logger.LogInformation(
                    "MPI auto-linked cross-source Certain match {Source} ↔ {Candidate} (score {Score:F3}).",
                    source.Id, match.Entry.Id, match.Score);
                continue;
            }

            _linkReviews.Add(PatientLinkReview.Raise(
                source.Id, source.PartnerId, Label(source),
                match.Entry.Id, match.Entry.PartnerId, Label(match.Entry),
                match.Score, match.Grade, now));
        }
    }

    private static string Label(PatientIndexEntry e) =>
        $"{e.FamilyName}, {e.GivenName} {(e.DateOfBirth?.ToString("yyyy-MM-dd") ?? "?")} [{e.PartnerId}]".Trim();

    private async Task EmitAsync<T>(T evt, CancellationToken cancellationToken)
    {
        if (_transponderOutbox is null) return;
        var json = JsonSerializer.Serialize(evt);
        var envelope = new TransponderOutboxEnvelope(typeof(T).AssemblyQualifiedName!, json);
        await _transponderOutbox.EnqueueAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    private static string ScopeFor(Resource resource) => resource.TypeName switch
    {
        "Patient" => ConsentScopes.Demographics,
        "Encounter" => ConsentScopes.Encounters,
        "DocumentReference" => ConsentScopes.ClinicalNotes,
        "Observation" or "DiagnosticReport" or "ServiceRequest" => ConsentScopes.Labs,
        "Procedure" or "AdverseEvent" => ConsentScopes.DialysisSessions,
        "MedicationStatement" or "MedicationRequest" => ConsentScopes.Medications,
        "AllergyIntolerance" => ConsentScopes.Allergies,
        "Condition" => ConsentScopes.Problems,
        _ => "general",
    };

    private static OperationOutcome.IssueComponent InformationIssue(string text) => new()
    {
        Severity = OperationOutcome.IssueSeverity.Information,
        Code = OperationOutcome.IssueType.Informational,
        Diagnostics = text,
    };
}
