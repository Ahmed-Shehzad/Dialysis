using Dialysis.CQRS.Queries;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListSessions;

public sealed record DialysisSessionListItem(
    Guid Id,
    Guid PatientId,
    string Status,
    DateTime ScheduledStartUtc,
    DateTime? ActualStartUtc,
    DateTime? ActualEndUtc,
    Guid? MachineId);

public sealed record ListSessionsQuery(bool ActiveOnly = false, int Take = 50)
    : IQuery<IReadOnlyList<DialysisSessionListItem>>, IPermissionedCommand
{
    public string RequiredPermission => PdmsPermissions.SessionRead;
}
