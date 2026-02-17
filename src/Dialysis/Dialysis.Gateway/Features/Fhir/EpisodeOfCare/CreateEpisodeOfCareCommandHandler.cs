using System.Text.Json;

using EpisodeOfCareEntity = Dialysis.Domain.Entities.EpisodeOfCare;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using global::Hl7.Fhir.Model;
using global::Hl7.Fhir.Serialization;

namespace Dialysis.Gateway.Features.Fhir.EpisodeOfCare;

public sealed class CreateEpisodeOfCareCommandHandler : ICommandHandler<CreateEpisodeOfCareCommand, CreateEpisodeOfCareResult>
{
    private static readonly JsonSerializerOptions FhirJsonOptions =
        new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector).Pretty();

    private readonly IEpisodeOfCareRepository _repository;
    private readonly ITenantContext _tenantContext;

    public CreateEpisodeOfCareCommandHandler(IEpisodeOfCareRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<CreateEpisodeOfCareResult> HandleAsync(CreateEpisodeOfCareCommand request, CancellationToken cancellationToken = default)
    {
        var fhir = JsonSerializer.Deserialize<global::Hl7.Fhir.Model.EpisodeOfCare>(request.FhirJson, FhirJsonOptions);
        if (fhir is null)
            return new CreateEpisodeOfCareResult(null, "Invalid FHIR EpisodeOfCare JSON.");

        var patientRef = fhir.Patient?.Reference;
        if (string.IsNullOrEmpty(patientRef) || !patientRef.Contains("Patient/"))
            return new CreateEpisodeOfCareResult(null, "Patient reference is required.");

        var patientIdStr = patientRef.Split("Patient/").LastOrDefault()?.Trim();
        if (string.IsNullOrEmpty(patientIdStr))
            return new CreateEpisodeOfCareResult(null, "Invalid Patient reference.");

        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(patientIdStr);
        var status = fhir.Status.ToString().ToLowerInvariant();
        DateTime? periodStart = null;
        DateTime? periodEnd = null;
        if (fhir.Period is not null)
        {
            if (!string.IsNullOrEmpty(fhir.Period.Start) && DateTime.TryParse(fhir.Period.Start, out var ps))
                periodStart = ps;
            if (!string.IsNullOrEmpty(fhir.Period.End) && DateTime.TryParse(fhir.Period.End, out var pe))
                periodEnd = pe;
        }
        var description = fhir.Type?.FirstOrDefault()?.Text;
        var diagnosisIds = fhir.Diagnosis?
            .Select(d => d.Condition?.Reference?.Split("Condition/").LastOrDefault())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>()
            .ToList() ?? [];

        var episode = EpisodeOfCareEntity.Create(
            tenantId,
            patientId,
            status,
            periodStart,
            periodEnd,
            description,
            diagnosisIds);

        await _repository.AddAsync(episode, cancellationToken);
        return new CreateEpisodeOfCareResult(episode, null);
    }
}
