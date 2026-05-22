using Dialysis.CQRS.Queries;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListChairAssignments;

/// <summary>
/// Wire shape for one current chair occupancy entry. Reusing <see cref="PdmsPermissions.SessionRead"/>
/// because the projection only reveals patient + chair + placement time — operationally adjacent
/// to session read, not a new PHI surface.
/// </summary>
public sealed record ChairAssignmentDto(string Chair, Guid PatientId, DateTime PlacedAtUtc);

public sealed record ListChairAssignmentsQuery()
    : IQuery<IReadOnlyList<ChairAssignmentDto>>, IPermissionedCommand
{
    public string RequiredPermission => PdmsPermissions.SessionRead;
}
