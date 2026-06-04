using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.EHR.PatientPortal.Ports;

namespace Dialysis.EHR.PatientPortal.Features.SendSecureMessage;

public sealed class SendSecureMessageCommandHandler : ICommandHandler<SendSecureMessageCommand, Guid>
{
    private readonly ISecureMessageRepository _messages;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public SendSecureMessageCommandHandler(ISecureMessageRepository messages,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _messages = messages;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
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
            _timeProvider.GetUtcNow().UtcDateTime);
        _messages.Add(message);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
