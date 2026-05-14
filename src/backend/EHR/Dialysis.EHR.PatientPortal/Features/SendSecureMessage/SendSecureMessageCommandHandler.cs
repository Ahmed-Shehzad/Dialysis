using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.EHR.PatientPortal.Ports;

namespace Dialysis.EHR.PatientPortal.Features.SendSecureMessage;

public sealed class SendSecureMessageCommandHandler(
    ISecureMessageRepository messages,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<SendSecureMessageCommand, Guid>
{
    public async Task<Guid> HandleAsync(SendSecureMessageCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var message = SecureMessage.Send(
            id,
            request.ThreadId ?? Guid.Empty,
            request.PatientId,
            request.TargetProviderId,
            request.Direction,
            request.Subject,
            request.Body,
            timeProvider.GetUtcNow().UtcDateTime);
        messages.Add(message);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
