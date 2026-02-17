using Dialysis.Persistence.Abstractions;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.DeviceIngestion.Features.Patients.Update;

public sealed class UpdatePatientHandler : ICommandHandler<UpdatePatientCommand, UpdatePatientResult>
{
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<UpdatePatientHandler> _logger;

    public UpdatePatientHandler(IPatientRepository patientRepository, ILogger<UpdatePatientHandler> logger)
    {
        _patientRepository = patientRepository;
        _logger = logger;
    }

    public async Task<UpdatePatientResult> HandleAsync(UpdatePatientCommand request, CancellationToken cancellationToken = default)
    {
        var patient = await _patientRepository.GetByIdAsync(request.TenantId, request.LogicalId, cancellationToken);
        if (patient is null)
            return null!;

        patient.Update(request.FamilyName, request.GivenNames, request.BirthDate);
        await _patientRepository.UpdateAsync(patient, cancellationToken);

        _logger.LogInformation(
            "Patient updated: LogicalId={LogicalId}, TenantId={TenantId}",
            request.LogicalId.Value,
            request.TenantId.Value);

        return new UpdatePatientResult(
            patient.LogicalId,
            patient.FamilyName,
            patient.GivenNames,
            patient.BirthDate);
    }
}
