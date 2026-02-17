using Dialysis.Contracts.Events;
using Dialysis.Domain.Entities;
using Dialysis.Persistence;
using Dialysis.Persistence.Abstractions;
using Dialysis.Persistence.Queries;
using Dialysis.SharedKernel.Exceptions;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.DeviceIngestion.Features.Patients.Create;

public sealed class CreatePatientHandler : ICommandHandler<CreatePatientCommand, CreatePatientResult>
{
    private readonly DialysisDbContext _db;
    private readonly IPatientRepository _patientRepository;
    private readonly IPublisher _publisher;
    private readonly ILogger<CreatePatientHandler> _logger;

    public CreatePatientHandler(DialysisDbContext db, IPatientRepository patientRepository, IPublisher publisher, ILogger<CreatePatientHandler> logger)
    {
        _db = db;
        _patientRepository = patientRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<CreatePatientResult> HandleAsync(CreatePatientCommand request, CancellationToken cancellationToken = default)
    {
        var tenantStr = request.TenantId.Value;
        var logicalIdStr = request.LogicalId.Value;
        if (await CompiledQueries.PatientExists(_db, tenantStr, logicalIdStr))
            throw new PatientAlreadyExistsException(tenantStr, logicalIdStr);

        var patient = Patient.Create(
            request.TenantId,
            request.LogicalId,
            request.FamilyName,
            request.GivenNames,
            request.BirthDate);

        await _patientRepository.AddAsync(patient, cancellationToken);

        await _publisher.PublishAsync(new PatientCreated(request.LogicalId.Value, request.TenantId), cancellationToken);

        _logger.LogInformation(
            "Patient created: LogicalId={LogicalId}, TenantId={TenantId}",
            request.LogicalId.Value,
            request.TenantId.Value);

        return new CreatePatientResult(request.LogicalId);
    }
}
