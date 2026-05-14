using System.Text.Json;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.Hie.Contracts.Integration;
using Dialysis.Hie.Core.Abstraction.Consent;
using Dialysis.Hie.Inbound.Domain;
using Dialysis.Hie.Inbound.Ports;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.Hie.Inbound.Ingestion;

/// <summary>
/// Validates, consent-gates, persists, and republishes inbound FHIR resources. Returns an
/// <see cref="OperationOutcome"/> describing the result for the FHIR controller to serialise back.
/// </summary>
public sealed class InboundIngestionService(
    IReceivedResourceStore resourceStore,
    IPatientIndex patientIndex,
    IConsentGate consentGate,
    ITransponderOutbox? transponderOutbox,
    TimeProvider timeProvider,
    ILogger<InboundIngestionService> logger)
{
    private static readonly FhirJsonSerializer _serializer = new();

    public async Task<OperationOutcome> IngestAsync(string partnerId, Resource resource, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partnerId);
        ArgumentNullException.ThrowIfNull(resource);

        var outcome = new OperationOutcome();
        if (resource is Bundle bundle)
        {
            foreach (var entry in bundle.Entry)
            {
                if (entry.Resource is null) continue;
                await IngestSingleAsync(partnerId, entry.Resource, outcome, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            await IngestSingleAsync(partnerId, resource, outcome, cancellationToken).ConfigureAwait(false);
        }

        await resourceStore.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (outcome.Issue.Count == 0)
            outcome.Issue.Add(InformationIssue("All entries accepted."));
        return outcome;
    }

    private async Task IngestSingleAsync(string partnerId, Resource resource, OperationOutcome outcome, CancellationToken cancellationToken)
    {
        var logicalId = resource.Id ?? Guid.NewGuid().ToString();
        var scope = ScopeFor(resource);

        var consented = await consentGate.CheckInboundAsync(logicalId, partnerId, scope, cancellationToken).ConfigureAwait(false);
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

        var fhirJson = await _serializer.SerializeToStringAsync(resource).ConfigureAwait(false);
        var received = new ReceivedResource(
            partnerId,
            resource.TypeName,
            logicalId,
            fhirJson,
            timeProvider.GetUtcNow().UtcDateTime,
            validationOutcome: "accepted");
        await resourceStore.UpsertAsync(received, cancellationToken).ConfigureAwait(false);

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
                    timeProvider.GetUtcNow().UtcDateTime,
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
                        valueQuantity = q.Value?.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        unit = q.Unit;
                    }
                    else if (observation.Value is FhirString fs)
                    {
                        valueQuantity = fs.Value;
                    }
                    var observed = observation.Effective is FhirDateTime fdt && DateTime.TryParse(fdt.Value, out var ts) ? ts : (DateTime?)null;
                    await EmitAsync(new ExternalLabResultIngestedIntegrationEvent(
                        Guid.NewGuid(),
                        timeProvider.GetUtcNow().UtcDateTime,
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
                        timeProvider.GetUtcNow().UtcDateTime,
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
                logger.LogDebug("No inbound projection implemented for {ResourceType}", resource.TypeName);
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
            .FirstOrDefault(i => string.Equals(i.System, Dialysis.Hie.Core.Coding.CodeSystems.MrnIdentifier, StringComparison.Ordinal))?.Value;

        var entry = new PatientIndexEntry(
            partnerId,
            logicalId,
            mrn,
            family,
            given,
            dob,
            patient.Gender?.ToString(),
            timeProvider.GetUtcNow().UtcDateTime);
        await patientIndex.UpsertAsync(entry, cancellationToken).ConfigureAwait(false);

        await EmitAsync(new ExternalPatientReferenceIngestedIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
            SchemaVersion: 1,
            partnerId,
            logicalId,
            mrn,
            family,
            given,
            dob,
            patient.Gender?.ToString()), cancellationToken).ConfigureAwait(false);
    }

    private async Task EmitAsync<T>(T evt, CancellationToken cancellationToken)
    {
        if (transponderOutbox is null) return;
        var json = JsonSerializer.Serialize(evt);
        var envelope = new TransponderOutboxEnvelope(typeof(T).AssemblyQualifiedName!, json);
        await transponderOutbox.EnqueueAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    private static string ScopeFor(Resource resource) => resource.TypeName switch
    {
        "Patient" => ConsentScopes.Demographics,
        "Encounter" => ConsentScopes.Encounters,
        "DocumentReference" => ConsentScopes.ClinicalNotes,
        "Observation" or "DiagnosticReport" or "ServiceRequest" => ConsentScopes.Labs,
        "Procedure" or "AdverseEvent" => ConsentScopes.DialysisSessions,
        _ => "general",
    };

    private static OperationOutcome.IssueComponent InformationIssue(string text) => new()
    {
        Severity = OperationOutcome.IssueSeverity.Information,
        Code = OperationOutcome.IssueType.Informational,
        Diagnostics = text,
    };
}
