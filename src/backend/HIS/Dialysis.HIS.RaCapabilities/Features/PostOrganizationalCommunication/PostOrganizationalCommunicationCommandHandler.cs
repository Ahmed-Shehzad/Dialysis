using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.PostOrganizationalCommunication;

public sealed class PostOrganizationalCommunicationCommandHandler : ICommandHandler<PostOrganizationalCommunicationCommand, Guid>
{
    private readonly IRaCapabilityCommandStore _store;
    private readonly IUnitOfWork _unitOfWork;
    public PostOrganizationalCommunicationCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    {
        _store = store;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(PostOrganizationalCommunicationCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        _store.AddOrganizationalCommunication(
            new RaOrgCommunication
            {
                Id = id,
                ThreadCode = request.ThreadCode.Trim(),
                Subject = request.Subject.Trim(),
                Body = request.Body.Trim(),
                SentAtUtc = DateTime.UtcNow,
            });
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
