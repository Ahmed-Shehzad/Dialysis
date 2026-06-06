using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.EHR.PatientPortal.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientPortal.Features.ReplySecureMessage;

/// <summary>A provider's reply to a patient on an existing secure-message thread.</summary>
public sealed record ProviderReplyToMessageCommand(
    Guid PatientId,
    Guid ThreadId,
    Guid ProviderId,
    string Subject,
    string Body) : ICommand<Guid>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PortalMessageReply;
}

public sealed class ProviderReplyToMessageCommandHandler : ICommandHandler<ProviderReplyToMessageCommand, Guid>
{
    private readonly ISecureMessageRepository _messages;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ProviderReplyToMessageCommandHandler(
        ISecureMessageRepository messages, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _messages = messages;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> HandleAsync(ProviderReplyToMessageCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var message = SecureMessage.Reply(
            id,
            request.ThreadId,
            request.PatientId,
            request.ProviderId,
            request.Subject,
            request.Body,
            _timeProvider.GetUtcNow().UtcDateTime);
        _messages.Add(message);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
