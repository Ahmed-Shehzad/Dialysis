using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.PostOrganizationalCommunication;

public sealed class PostOrganizationalCommunicationCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    : ICommandHandler<PostOrganizationalCommunicationCommand, Guid>
{
    public async Task<Guid> Handle(PostOrganizationalCommunicationCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        store.AddOrganizationalCommunication(
            new RaOrgCommunication
            {
                Id = id,
                ThreadCode = request.ThreadCode.Trim(),
                Subject = request.Subject.Trim(),
                Body = request.Body.Trim(),
                SentAtUtc = DateTime.UtcNow,
            });
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
