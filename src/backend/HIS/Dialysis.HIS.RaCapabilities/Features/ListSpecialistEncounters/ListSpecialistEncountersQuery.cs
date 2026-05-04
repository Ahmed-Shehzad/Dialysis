using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.ListSpecialistEncounters;

public sealed record ListSpecialistEncountersQuery
    : IQuery<IReadOnlyList<RaSpecialistEncounterRow>>, IPermissionedCommand
{
    public string RequiredPermission => HisPermissions.RaCapabilitiesRead;
}
