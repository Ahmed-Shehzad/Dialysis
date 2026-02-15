using System.Text;
using Task = System.Threading.Tasks.Task;
using Dialysis.ApiClients;
using Dialysis.Analytics.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Intercessor.Abstractions;

namespace Dialysis.Analytics.Features.Export;

public sealed class ExportCohortCommandHandler : ICommandHandler<ExportCohortCommand>
{
    private readonly IFhirApi _fhirApi;
    private readonly IAnalyticsAuditRecorder _audit;

    public ExportCohortCommandHandler(IFhirApi fhirApi, IAnalyticsAuditRecorder audit)
    {
        _fhirApi = fhirApi;
        _audit = audit;
    }

    public async Task HandleAsync(ExportCohortCommand request, CancellationToken cancellationToken = default)
    {
        var (cohort, resourceType, format, output) = request;

        if (resourceType == "Patient" && cohort.PatientIds.Count > 0)
        {
            foreach (var id in cohort.PatientIds)
            {
                try
                {
                    var resource = await _fhirApi.GetPatient(id, cancellationToken);
                    if (format == ExportFormat.NdJson)
                    {
                        var line = new FhirJsonSerializer().SerializeToString(resource) + "\n";
                        await output.WriteAsync(Encoding.UTF8.GetBytes(line), cancellationToken);
                    }
                }
                catch
                {
                    // Skip if not found
                }
            }
        }
        else if (resourceType == "Encounter" && cohort.EncounterIds.Count > 0)
        {
            foreach (var id in cohort.EncounterIds)
            {
                try
                {
                    var resource = await _fhirApi.GetEncounter(id, cancellationToken);
                    if (format == ExportFormat.NdJson)
                    {
                        var line = new FhirJsonSerializer().SerializeToString(resource) + "\n";
                        await output.WriteAsync(Encoding.UTF8.GetBytes(line), cancellationToken);
                    }
                }
                catch
                {
                    // Skip if not found
                }
            }
        }

        await _audit.RecordAsync("Export", "cohort", "read", outcome: "0", cancellationToken: cancellationToken);
    }
}
