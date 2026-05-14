using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.RecordSecurityMechanismAssessment;

public sealed class RecordSecurityMechanismAssessmentCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    : ICommandHandler<RecordSecurityMechanismAssessmentCommand, Guid>
{
    public async Task<Guid> HandleAsync(RecordSecurityMechanismAssessmentCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        store.AddSecurityMechanismHardening(
            new RaSecurityMechanismHardening
            {
                Id = id,
                MechanismCode = request.MechanismCode.Trim(),
                AppliedLevel = request.AppliedLevel.Trim(),
                Notes = request.Notes.Trim(),
                AssessedAtUtc = DateTime.UtcNow,
            });
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
