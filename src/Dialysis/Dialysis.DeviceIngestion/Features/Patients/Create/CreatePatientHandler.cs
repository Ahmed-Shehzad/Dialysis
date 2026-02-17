using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Exceptions;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.DeviceIngestion.Features.Patients.Create;

public sealed class CreatePatientHandler : ICommandHandler<CreatePatientCommand, CreatePatientResult>
{
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<CreatePatientHandler> _logger;

    public CreatePatientHandler(IPatientRepository patientRepository, ILogger<CreatePatientHandler> logger)
    {
        _patientRepository = patientRepository;
        _logger = logger;
    }

    public async Task<CreatePatientResult> HandleAsync(CreatePatientCommand request, CancellationToken cancellationToken = default)
    {
        if (await _patientRepository.ExistsAsync(request.TenantId, request.LogicalId, cancellationToken))
            throw new PatientAlreadyExistsException(request.TenantId.Value, request.LogicalId.Value);

        var patient = Patient.Create(
            request.TenantId,
            request.LogicalId,
            request.FamilyName,
            request.GivenNames,
            request.BirthDate);

        await _patientRepository.AddAsync(patient, cancellationToken);

        _logger.LogInformation(
            "Patient created: LogicalId={LogicalId}, TenantId={TenantId}",
            request.LogicalId.Value,
            request.TenantId.Value);

        return new CreatePatientResult(request.LogicalId);
    }
}
