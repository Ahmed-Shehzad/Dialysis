using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.EnqueueWaitlistEntry;

public sealed class EnqueueWaitlistEntryCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    : ICommandHandler<EnqueueWaitlistEntryCommand, Guid>
{
    public async Task<Guid> HandleAsync(EnqueueWaitlistEntryCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        store.AddWaitlistEntry(
            new RaWaitlistEntry
            {
                Id = id,
                PatientId = request.PatientId,
                ResourceKindCode = request.ResourceKindCode.Trim(),
                Notes = request.Notes.Trim(),
                RequestedNotBeforeUtc = request.RequestedNotBeforeUtc,
                EnqueuedAtUtc = DateTime.UtcNow,
            });
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
