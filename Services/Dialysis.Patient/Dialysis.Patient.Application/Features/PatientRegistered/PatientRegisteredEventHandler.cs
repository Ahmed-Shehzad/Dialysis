using BuildingBlocks.Abstractions;

using Dialysis.Patient.Application.Domain.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Patient.Application.Features.PatientRegistered;

internal sealed class PatientRegisteredEventHandler : IDomainEventHandler<PatientRegisteredEvent>
{
    private readonly ILogger<PatientRegisteredEventHandler> _logger;
    private readonly IAuditRecorder _audit;

    public PatientRegisteredEventHandler(ILogger<PatientRegisteredEventHandler> logger, IAuditRecorder audit)
    {
        _logger = logger;
        _audit = audit;
    }

    public async Task HandleAsync(PatientRegisteredEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DomainEvent: PatientRegistered PatientId={PatientId} MRN={Mrn} Name={FirstName} {LastName}",
            notification.PatientId,
            notification.MedicalRecordNumber.Value,
            notification.Name.FirstName,
            notification.Name.LastName);

        await _audit.RecordAsync(new AuditRecordRequest(
            AuditAction.Create,
            "Patient",
            notification.PatientId.ToString(),
            null,
            AuditOutcome.Success,
            "Patient registered (domain event)",
            null),
            cancellationToken);
    }
}
