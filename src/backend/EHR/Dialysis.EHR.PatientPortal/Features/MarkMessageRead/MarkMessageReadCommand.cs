using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientPortal.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientPortal.Features.MarkMessageRead;

/// <summary>Marks a secure message read (idempotent — a no-op once already read).</summary>
public sealed record MarkMessageReadCommand(Guid MessageId) : ICommand<Unit>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PortalMessageRead;
}

public sealed class MarkMessageReadCommandHandler : ICommandHandler<MarkMessageReadCommand, Unit>
{
    private readonly ISecureMessageRepository _messages;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public MarkMessageReadCommandHandler(
        ISecureMessageRepository messages, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _messages = messages;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> HandleAsync(MarkMessageReadCommand request, CancellationToken cancellationToken)
    {
        var message = await _messages.GetAsync(request.MessageId, cancellationToken).ConfigureAwait(false);
        if (message is null)
            return Unit.Value;

        message.MarkRead(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
