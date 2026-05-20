using Dialysis.CQRS.Commands;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.PatientFlow.Features.GetTodaysQueue;
using Dialysis.HIS.PatientFlow.Ports;

namespace Dialysis.HIS.PatientFlow.Features.RegisterWalkIn;

public sealed class RegisterWalkInCommandHandler(IPatientQueueRepository repository)
    : ICommandHandler<RegisterWalkInCommand, PatientQueueEntryDto>
{
    public Task<PatientQueueEntryDto> HandleAsync(
        RegisterWalkInCommand request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var entry = PatientQueueEntry.WalkIn(
            id: Guid.NewGuid(),
            patientId: Guid.NewGuid(),
            patientName: request.PatientName.Trim(),
            mrn: request.Mrn.Trim(),
            arrivalUtc: now,
            eligibilityVerified: request.EligibilityVerified);
        repository.Add(entry);
        return Task.FromResult(new PatientQueueEntryDto(
            entry.Id,
            entry.PatientId,
            entry.PatientName,
            entry.Mrn,
            entry.ScheduledForUtc,
            "waiting",
            entry.Chair,
            entry.EligibilityVerified));
    }
}
