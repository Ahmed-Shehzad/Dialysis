using Dialysis.Domain.Aggregates;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Gateway.Features.Meds;

public sealed class CreateMedicationCommandHandler : ICommandHandler<CreateMedicationCommand, CreateMedicationResult>
{
    private readonly Dialysis.Persistence.DialysisDbContext _db;
    private readonly IMedicationAdministrationRepository _repository;
    private readonly ITenantContext _tenantContext;

    public CreateMedicationCommandHandler(
        Dialysis.Persistence.DialysisDbContext db,
        IMedicationAdministrationRepository repository,
        ITenantContext tenantContext)
    {
        _db = db;
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<CreateMedicationResult> HandleAsync(CreateMedicationCommand request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(request.PatientId);

        var patientExists = await _db.Patients
            .AnyAsync(p => p.TenantId == tenantId && p.LogicalId == patientId, cancellationToken);
        if (!patientExists)
            return new CreateMedicationResult(null, "Patient not found.");

        var medication = MedicationAdministration.Create(
            tenantId,
            patientId,
            request.MedicationCode,
            request.MedicationDisplay,
            request.DoseQuantity,
            request.DoseUnit,
            request.Route,
            request.EffectiveAt,
            request.SessionId,
            request.ReasonText,
            request.PerformerId);

        await _repository.AddAsync(medication, cancellationToken);
        return new CreateMedicationResult(medication, null);
    }
}
