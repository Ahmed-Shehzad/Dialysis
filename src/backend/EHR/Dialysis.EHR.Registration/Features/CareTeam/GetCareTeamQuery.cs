using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.Registration.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Registration.Features.CareTeam;

/// <summary>Returns the patient's care team (members), or null when none exists.</summary>
public sealed record GetCareTeamQuery(Guid PatientId) : IQuery<CareTeamView?>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.CareTeamRead;
}

/// <summary>A care team projected for the chart card.</summary>
public sealed record CareTeamView(Guid Id, Guid PatientId, IReadOnlyList<CareTeamMemberView> Members);

/// <summary>A care-team member projected for the chart card.</summary>
public sealed record CareTeamMemberView(Guid ProviderId, string Role, bool IsPrimary);

public sealed class GetCareTeamQueryHandler : IQueryHandler<GetCareTeamQuery, CareTeamView?>
{
    private readonly ICareTeamRepository _careTeams;
    public GetCareTeamQueryHandler(ICareTeamRepository careTeams) => _careTeams = careTeams;

    public async Task<CareTeamView?> HandleAsync(GetCareTeamQuery request, CancellationToken cancellationToken)
    {
        var team = await _careTeams.GetByPatientAsync(request.PatientId, cancellationToken).ConfigureAwait(false);
        if (team is null)
            return null;
        return new CareTeamView(team.Id, team.PatientId,
            [.. team.Members
                .OrderByDescending(m => m.IsPrimary)
                .Select(m => new CareTeamMemberView(m.ProviderId, m.Role.ToString(), m.IsPrimary))]);
    }
}
