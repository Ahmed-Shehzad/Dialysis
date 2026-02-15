using System.Text;
using Dialysis.Analytics.Data;
using Dialysis.Analytics.Features.Cohorts;
using Dialysis.Analytics.Services;
using Dialysis.ApiClients;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Research;

public sealed class ResearchExportHandler : ICommandHandler<ResearchExportCommand, ResearchExportResult>
{
    private readonly ISavedCohortStore _store;
    private readonly ISender _sender;
    private readonly IFhirApi _fhirApi;
    private readonly IDeidentificationApiClient _deidentifyClient;
    private readonly IAnalyticsAuditRecorder _audit;
    private readonly IConsentVerificationClient _consent;

    public ResearchExportHandler(
        ISavedCohortStore store,
        ISender sender,
        IFhirApi fhirApi,
        IAnalyticsAuditRecorder audit,
        IDeidentificationApiClient deidentifyClient,
        IConsentVerificationClient consent)
    {
        _store = store;
        _sender = sender;
        _fhirApi = fhirApi;
        _audit = audit;
        _deidentifyClient = deidentifyClient;
        _consent = consent;
    }

    public async Task<ResearchExportResult> HandleAsync(ResearchExportCommand request, CancellationToken cancellationToken = default)
    {
        CohortResult cohortResult;
        string cohortId;

        if (!string.IsNullOrEmpty(request.CohortId))
        {
            var cohort = await _store.GetByIdAsync(request.CohortId, cancellationToken);
            if (cohort == null)
                return new ResearchExportResult(false, 0, "Cohort not found");

            var hasConsent = await _consent.HasConsentAsync("Consent", request.CohortId, "research-export", cancellationToken);
            if (!hasConsent)
                return new ResearchExportResult(false, 0, "Consent required for research export");

            var resolved = await _sender.SendAsync(new ResolveSavedCohortQuery(request.CohortId), cancellationToken);
            if (resolved == null)
                return new ResearchExportResult(false, 0, "Failed to resolve cohort");

            cohortResult = new CohortResult
            {
                PatientIds = resolved.PatientIds,
                EncounterIds = resolved.EncounterIds
            };
            cohortId = request.CohortId;
        }
        else if (request.Criteria != null)
        {
            var hasConsent = await _consent.HasConsentAsync("Consent", "criteria", "research-export", cancellationToken);
            if (!hasConsent)
                return new ResearchExportResult(false, 0, "Consent required for criteria-based research export");

            var resolved = await _sender.SendAsync(new ResolveCohortQuery(request.Criteria), cancellationToken);
            cohortResult = resolved;
            cohortId = "criteria";
        }
        else
        {
            return new ResearchExportResult(false, 0, "Provide cohortId or criteria");
        }

        var output = request.Output ?? throw new InvalidOperationException("Output stream is required");
        long count = 0;

        if (request.ResourceType == "Patient" && cohortResult.PatientIds.Count > 0)
        {
            foreach (var id in cohortResult.PatientIds)
            {
                try
                {
                    var resource = await _fhirApi.GetPatient(id, cancellationToken);
                    var json = await ProcessResourceAsync(resource, request.Level, cancellationToken);
                    if (json != null)
                    {
                        await output.WriteAsync(Encoding.UTF8.GetBytes(json + "\n"), cancellationToken);
                        count++;
                    }
                }
                catch
                {
                    // Skip if not found
                }
            }
        }
        else if (request.ResourceType == "Encounter" && cohortResult.EncounterIds.Count > 0)
        {
            foreach (var id in cohortResult.EncounterIds)
            {
                try
                {
                    var resource = await _fhirApi.GetEncounter(id, cancellationToken);
                    var json = await ProcessResourceAsync(resource, request.Level, cancellationToken);
                    if (json != null)
                    {
                        await output.WriteAsync(Encoding.UTF8.GetBytes(json + "\n"), cancellationToken);
                        count++;
                    }
                }
                catch
                {
                    // Skip if not found
                }
            }
        }

        await _audit.RecordAsync("Export", $"research-{cohortId}", "read", outcome: "0", cancellationToken: cancellationToken);
        return new ResearchExportResult(true, count, null);
    }

    private async Task<string?> ProcessResourceAsync(Resource resource, string level, CancellationToken cancellationToken)
    {
        var input = new MemoryStream(Encoding.UTF8.GetBytes(new FhirJsonSerializer().SerializeToString(resource)));
        var deidentified = await _deidentifyClient.DeidentifyAsync(input, level, cancellationToken);
        if (deidentified != null)
        {
            using var reader = new StreamReader(deidentified);
            return await reader.ReadToEndAsync(cancellationToken);
        }
        return new FhirJsonSerializer().SerializeToString(resource);
    }
}
