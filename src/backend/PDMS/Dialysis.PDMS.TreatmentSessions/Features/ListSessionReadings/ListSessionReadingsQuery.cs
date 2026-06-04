using Dialysis.CQRS.Queries;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;
using Dialysis.PDMS.TreatmentSessions.Realtime;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListSessionReadings;

public sealed record ListSessionReadingsQuery : IQuery<IReadOnlyList<VitalsReadingSnapshot>>, IPermissionedCommand
{
    public ListSessionReadingsQuery(Guid SessionId, int Limit = 200)
    {
        this.SessionId = SessionId;
        this.Limit = Limit;
    }
    public string RequiredPermission => PdmsPermissions.ReadingRead;
    public Guid SessionId { get; init; }
    public int Limit { get; init; }
    public void Deconstruct(out Guid SessionId, out int Limit)
    {
        SessionId = this.SessionId;
        Limit = this.Limit;
    }
}
