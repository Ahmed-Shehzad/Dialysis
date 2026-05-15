using Dialysis.CQRS.Queries;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;
using Dialysis.PDMS.TreatmentSessions.Realtime;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListSessionReadings;

public sealed record ListSessionReadingsQuery(Guid SessionId, int Limit = 200)
    : IQuery<IReadOnlyList<VitalsReadingSnapshot>>, IPermissionedCommand
{
    public string RequiredPermission => PdmsPermissions.ReadingRead;
}
