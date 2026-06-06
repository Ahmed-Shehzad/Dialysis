using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.EHR.PatientPortal.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientPortal.Features.ListMessageThreads;

/// <summary>A secure-message thread summarised for the inbox list (patient and provider share the shape).</summary>
public sealed record MessageThreadView(
    Guid ThreadId,
    string Subject,
    DateTime LastMessageAtUtc,
    string LastDirection,
    int MessageCount,
    int UnreadFromCareTeam);

/// <summary>Lists a patient's secure-message threads, newest activity first.</summary>
public sealed record ListMessageThreadsForPatientQuery(Guid PatientId)
    : IQuery<IReadOnlyList<MessageThreadView>>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PortalMessageRead;
}

public sealed class ListMessageThreadsForPatientQueryHandler
    : IQueryHandler<ListMessageThreadsForPatientQuery, IReadOnlyList<MessageThreadView>>
{
    private readonly ISecureMessageRepository _messages;
    public ListMessageThreadsForPatientQueryHandler(ISecureMessageRepository messages) => _messages = messages;

    public async Task<IReadOnlyList<MessageThreadView>> HandleAsync(
        ListMessageThreadsForPatientQuery request, CancellationToken cancellationToken)
    {
        var all = await _messages.ListByPatientAsync(request.PatientId, cancellationToken).ConfigureAwait(false);
        return
        [
            .. all
                .GroupBy(m => m.ThreadId)
                .Select(g =>
                {
                    var ordered = g.OrderBy(m => m.SentAtUtc).ToList();
                    var last = ordered[^1];
                    return new MessageThreadView(
                        g.Key,
                        ordered[0].Subject,
                        last.SentAtUtc,
                        last.Direction.ToString(),
                        ordered.Count,
                        // From the patient's perspective, "unread" is an unacknowledged care-team reply.
                        ordered.Count(m => m.Direction == SecureMessageDirection.ProviderToPatient && m.ReadAtUtc is null));
                })
                .OrderByDescending(t => t.LastMessageAtUtc),
        ];
    }
}
