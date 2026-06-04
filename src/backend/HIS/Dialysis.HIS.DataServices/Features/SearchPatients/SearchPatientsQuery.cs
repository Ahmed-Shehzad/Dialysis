using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.DataServices.Ports;

namespace Dialysis.HIS.DataServices.Features.SearchPatients;

public sealed record SearchPatientsQuery : IQuery<IReadOnlyList<PatientSearchRow>>, IPermissionedCommand
{
    public SearchPatientsQuery(string? Q = null, int Skip = 0, int Take = 50)
    {
        this.Q = Q;
        this.Skip = Skip;
        this.Take = Take;
    }
    public string RequiredPermission => HisPermissions.DataSearch;
    public string? Q { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; }
    public void Deconstruct(out string? Q, out int Skip, out int Take)
    {
        Q = this.Q;
        Skip = this.Skip;
        Take = this.Take;
    }
}
