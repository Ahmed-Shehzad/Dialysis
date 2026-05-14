using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.SearchPatients;

public sealed record SearchPatientsQuery(string? Q = null, int Skip = 0, int Take = 50)
    : IQuery<IReadOnlyList<PatientSearchRow>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.DataSearch;
}
