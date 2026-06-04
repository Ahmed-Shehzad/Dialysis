using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.RecordSecurityMechanismAssessment;

public sealed class RecordSecurityMechanismAssessmentCommandHandler : ICommandHandler<RecordSecurityMechanismAssessmentCommand, Guid>
{
    private readonly IRaCapabilityCommandStore _store;
    private readonly IUnitOfWork _unitOfWork;
    public RecordSecurityMechanismAssessmentCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    {
        _store = store;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RecordSecurityMechanismAssessmentCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        _store.AddSecurityMechanismHardening(
            new RaSecurityMechanismHardening
            {
                Id = id,
                MechanismCode = request.MechanismCode.Trim(),
                AppliedLevel = request.AppliedLevel.Trim(),
                Notes = request.Notes.Trim(),
                AssessedAtUtc = DateTime.UtcNow,
            });
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
