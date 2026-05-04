using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterSpecialistEncounter;

public sealed class RegisterSpecialistEncounterCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterSpecialistEncounterCommand, Guid>
{
    public async Task<Guid> Handle(RegisterSpecialistEncounterCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var at = request.RecordedAtUtc ?? DateTime.UtcNow;
        store.AddSpecialistEncounterRecord(
            new RaSpecialistEncounterRecord
            {
                Id = id,
                PatientId = request.PatientId,
                SpecialtyCode = request.SpecialtyCode.Trim(),
                ExternalSystemCode = request.ExternalSystemCode.Trim(),
                Summary = request.Summary.Trim(),
                RecordedAtUtc = at,
            });
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
