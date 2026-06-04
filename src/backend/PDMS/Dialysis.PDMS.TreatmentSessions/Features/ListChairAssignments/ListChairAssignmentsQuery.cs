using Dialysis.CQRS.Queries;
using Dialysis.Module.Contracts.Authorization;
using Dialysis.PDMS.Contracts.Security;

namespace Dialysis.PDMS.TreatmentSessions.Features.ListChairAssignments;

/// <summary>
/// Wire shape for one current chair occupancy entry. Reusing <see cref="PdmsPermissions.SessionRead"/>
/// because the projection only reveals patient + chair + placement time — operationally adjacent
/// to session read, not a new PHI surface.
/// </summary>
public sealed record ChairAssignmentDto
{
    /// <summary>
    /// Wire shape for one current chair occupancy entry. Reusing <see cref="PdmsPermissions.SessionRead"/>
    /// because the projection only reveals patient + chair + placement time — operationally adjacent
    /// to session read, not a new PHI surface.
    /// </summary>
    public ChairAssignmentDto(string Chair, Guid PatientId, DateTime PlacedAtUtc)
    {
        this.Chair = Chair;
        this.PatientId = PatientId;
        this.PlacedAtUtc = PlacedAtUtc;
    }
    public string Chair { get; init; }
    public Guid PatientId { get; init; }
    public DateTime PlacedAtUtc { get; init; }
    public void Deconstruct(out string Chair, out Guid PatientId, out DateTime PlacedAtUtc)
    {
        Chair = this.Chair;
        PatientId = this.PatientId;
        PlacedAtUtc = this.PlacedAtUtc;
    }
}

public sealed record ListChairAssignmentsQuery : IQuery<IReadOnlyList<ChairAssignmentDto>>, IPermissionedCommand
{
    public ListChairAssignmentsQuery()
    {
    }
    public string RequiredPermission => PdmsPermissions.SessionRead;
}
