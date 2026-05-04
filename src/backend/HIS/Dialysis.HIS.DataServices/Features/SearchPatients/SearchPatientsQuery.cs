using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.SearchPatients;

public sealed record SearchPatientsQuery(string? MrnContains)
    : IQuery<IReadOnlyList<PatientSearchResultDto>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.DataSearch;
}
