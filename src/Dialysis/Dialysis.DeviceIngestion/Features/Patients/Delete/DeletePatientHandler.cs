using Dialysis.Persistence.Abstractions;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.DeviceIngestion.Features.Patients.Delete;

public sealed class DeletePatientHandler : ICommandHandler<DeletePatientCommand, bool>
{
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<DeletePatientHandler> _logger;

    public DeletePatientHandler(IPatientRepository patientRepository, ILogger<DeletePatientHandler> logger)
    {
        _patientRepository = patientRepository;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(DeletePatientCommand request, CancellationToken cancellationToken = default)
    {
        var patient = await _patientRepository.GetByIdAsync(request.TenantId, request.LogicalId, cancellationToken);
        if (patient is null)
            return false;

        await _patientRepository.DeleteAsync(patient, cancellationToken);

        _logger.LogInformation(
            "Patient deleted: LogicalId={LogicalId}, TenantId={TenantId}",
            request.LogicalId.Value,
            request.TenantId.Value);

        return true;
    }
}
