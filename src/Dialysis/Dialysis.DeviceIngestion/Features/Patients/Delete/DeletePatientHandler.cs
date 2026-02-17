using Dialysis.Persistence;
using Dialysis.Persistence.Abstractions;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dialysis.DeviceIngestion.Features.Patients.Delete;

public sealed class DeletePatientHandler : ICommandHandler<DeletePatientCommand, bool>
{
    private readonly DialysisDbContext _db;
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<DeletePatientHandler> _logger;

    public DeletePatientHandler(DialysisDbContext db, IPatientRepository patientRepository, ILogger<DeletePatientHandler> logger)
    {
        _db = db;
        _patientRepository = patientRepository;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(DeletePatientCommand request, CancellationToken cancellationToken = default)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.TenantId == request.TenantId && p.LogicalId == request.LogicalId, cancellationToken);
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
