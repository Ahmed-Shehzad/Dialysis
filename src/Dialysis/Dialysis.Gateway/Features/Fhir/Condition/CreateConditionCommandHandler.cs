using System.Text.Json;

using ConditionEntity = Dialysis.Domain.Entities.Condition;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using global::Hl7.Fhir.Model;
using global::Hl7.Fhir.Serialization;

namespace Dialysis.Gateway.Features.Fhir.Condition;

public sealed class CreateConditionCommandHandler : ICommandHandler<CreateConditionCommand, CreateConditionResult>
{
    private static readonly JsonSerializerOptions FhirJsonOptions =
        new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector).Pretty();

    private readonly IConditionRepository _repository;
    private readonly ITenantContext _tenantContext;

    public CreateConditionCommandHandler(IConditionRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<CreateConditionResult> HandleAsync(CreateConditionCommand request, CancellationToken cancellationToken = default)
    {
        var fhir = JsonSerializer.Deserialize<global::Hl7.Fhir.Model.Condition>(request.FhirJson, FhirJsonOptions);
        if (fhir is null)
            return new CreateConditionResult(null, "Invalid FHIR Condition JSON.");

        var subjectRef = fhir.Subject?.Reference;
        if (string.IsNullOrEmpty(subjectRef) || !subjectRef.Contains("Patient/"))
            return new CreateConditionResult(null, "Subject reference to Patient is required.");

        var patientIdStr = subjectRef.Split("Patient/").LastOrDefault()?.Trim();
        if (string.IsNullOrEmpty(patientIdStr))
            return new CreateConditionResult(null, "Invalid Patient reference.");

        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(patientIdStr);
        var code = fhir.Code?.Coding?.FirstOrDefault();
        var codeSystem = code?.System ?? "http://snomed.info/sct";
        var codeValue = code?.Code ?? "unknown";
        var display = code?.Display ?? fhir.Code?.Text ?? codeValue;
        var clinicalStatus = fhir.ClinicalStatus?.Coding?.FirstOrDefault()?.Code ?? "active";
        var verificationStatus = fhir.VerificationStatus?.Coding?.FirstOrDefault()?.Code ?? "confirmed";
        DateTime? onset = null;
        if (fhir.Onset is FhirDateTime fd && DateTime.TryParse(fd.Value, out var d))
            onset = d;

        var condition = ConditionEntity.Create(
            tenantId,
            patientId,
            codeSystem,
            codeValue,
            display,
            clinicalStatus,
            verificationStatus,
            onset);

        await _repository.AddAsync(condition, cancellationToken);
        return new CreateConditionResult(condition, null);
    }
}
