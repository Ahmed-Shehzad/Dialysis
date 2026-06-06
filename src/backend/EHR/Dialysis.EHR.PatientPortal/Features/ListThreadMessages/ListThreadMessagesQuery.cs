using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientPortal.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientPortal.Features.ListThreadMessages;

/// <summary>A single secure message projected for the thread view.</summary>
public sealed record SecureMessageView(
    Guid Id,
    Guid ThreadId,
    Guid PatientId,
    string Direction,
    string Subject,
    string Body,
    DateTime SentAtUtc,
    DateTime? ReadAtUtc);

/// <summary>Lists the messages in a thread, oldest first.</summary>
public sealed record ListThreadMessagesQuery(Guid ThreadId)
    : IQuery<IReadOnlyList<SecureMessageView>>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PortalMessageRead;
}

public sealed class ListThreadMessagesQueryHandler
    : IQueryHandler<ListThreadMessagesQuery, IReadOnlyList<SecureMessageView>>
{
    private readonly ISecureMessageRepository _messages;
    public ListThreadMessagesQueryHandler(ISecureMessageRepository messages) => _messages = messages;

    public async Task<IReadOnlyList<SecureMessageView>> HandleAsync(
        ListThreadMessagesQuery request, CancellationToken cancellationToken)
    {
        var messages = await _messages.ListByThreadAsync(request.ThreadId, cancellationToken).ConfigureAwait(false);
        return
        [
            .. messages.Select(m => new SecureMessageView(
                m.Id, m.ThreadId, m.PatientId, m.Direction.ToString(), m.Subject, m.Body, m.SentAtUtc, m.ReadAtUtc)),
        ];
    }
}
