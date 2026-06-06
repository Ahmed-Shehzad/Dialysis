using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.Registration.Domain;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Registration.Features.CareTeam;

/// <summary>Adds a provider to a patient's care team (creating the team on first member). Returns the member id.</summary>
public sealed record AddCareTeamMemberCommand(Guid PatientId, Guid ProviderId, CareTeamRole Role, bool IsPrimary)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.CareTeamManage;
}

/// <summary>Removes a provider from a patient's care team.</summary>
public sealed record RemoveCareTeamMemberCommand(Guid PatientId, Guid ProviderId)
    : ICommand<Unit>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.CareTeamManage;
}

/// <summary>Designates a provider as the care team's primary (demoting any current primary).</summary>
public sealed record SetPrimaryCareTeamMemberCommand(Guid PatientId, Guid ProviderId)
    : ICommand<Unit>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.CareTeamManage;
}
