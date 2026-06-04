using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterResearchEducationActivity;

public sealed class RegisterResearchEducationActivityCommandHandler : ICommandHandler<RegisterResearchEducationActivityCommand, Guid>
{
    private readonly IRaCapabilityCommandStore _store;
    private readonly IUnitOfWork _unitOfWork;
    public RegisterResearchEducationActivityCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    {
        _store = store;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RegisterResearchEducationActivityCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var at = request.RecordedAtUtc ?? DateTime.UtcNow;
        _store.AddResearchEducationActivity(
            new RaResearchEducationActivity
            {
                Id = id,
                ActivityKindCode = request.ActivityKindCode.Trim().ToLowerInvariant(),
                Title = request.Title.Trim(),
                ExternalReference = request.ExternalReference.Trim(),
                RecordedAtUtc = at,
            });
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
