using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.EnqueueWaitlistEntry;

public sealed class EnqueueWaitlistEntryCommandHandler : ICommandHandler<EnqueueWaitlistEntryCommand, Guid>
{
    private readonly IRaCapabilityCommandStore _store;
    private readonly IUnitOfWork _unitOfWork;
    public EnqueueWaitlistEntryCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    {
        _store = store;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(EnqueueWaitlistEntryCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        _store.AddWaitlistEntry(
            new RaWaitlistEntry
            {
                Id = id,
                PatientId = request.PatientId,
                ResourceKindCode = request.ResourceKindCode.Trim(),
                Notes = request.Notes.Trim(),
                RequestedNotBeforeUtc = request.RequestedNotBeforeUtc,
                EnqueuedAtUtc = DateTime.UtcNow,
            });
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
