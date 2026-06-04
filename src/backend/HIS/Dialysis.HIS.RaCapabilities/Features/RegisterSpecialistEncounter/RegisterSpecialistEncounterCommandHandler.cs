using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterSpecialistEncounter;

public sealed class RegisterSpecialistEncounterCommandHandler : ICommandHandler<RegisterSpecialistEncounterCommand, Guid>
{
    private readonly IRaCapabilityCommandStore _store;
    private readonly IUnitOfWork _unitOfWork;
    public RegisterSpecialistEncounterCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    {
        _store = store;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RegisterSpecialistEncounterCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var at = request.RecordedAtUtc ?? DateTime.UtcNow;
        _store.AddSpecialistEncounterRecord(
            new RaSpecialistEncounterRecord
            {
                Id = id,
                PatientId = request.PatientId,
                SpecialtyCode = request.SpecialtyCode.Trim(),
                ExternalSystemCode = request.ExternalSystemCode.Trim(),
                Summary = request.Summary.Trim(),
                RecordedAtUtc = at,
            });
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
