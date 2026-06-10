using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.PatientFlow.Features.GetTodaysQueue;

public sealed record GetTodaysQueueQuery : IQuery<IReadOnlyList<PatientQueueEntryDto>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.PatientFlowQueueRead;
}
