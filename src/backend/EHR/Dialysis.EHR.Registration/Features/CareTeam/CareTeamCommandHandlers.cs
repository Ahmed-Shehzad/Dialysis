using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Registration.Ports;
using DomainCareTeam = Dialysis.EHR.Registration.Domain.CareTeam;

namespace Dialysis.EHR.Registration.Features.CareTeam;

public sealed class AddCareTeamMemberCommandHandler : ICommandHandler<AddCareTeamMemberCommand, Guid>
{
    private readonly ICareTeamRepository _careTeams;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    public AddCareTeamMemberCommandHandler(ICareTeamRepository careTeams, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _careTeams = careTeams;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }
    public async Task<Guid> HandleAsync(AddCareTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await _careTeams.GetByPatientAsync(request.PatientId, cancellationToken).ConfigureAwait(false);
        if (team is null)
        {
            team = DomainCareTeam.Create(Guid.CreateVersion7(), request.PatientId, _timeProvider.GetUtcNow().UtcDateTime);
            _careTeams.Add(team);
        }
        var memberId = Guid.CreateVersion7();
        team.AddMember(memberId, request.ProviderId, request.Role, request.IsPrimary);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return memberId;
    }
}

public sealed class RemoveCareTeamMemberCommandHandler : ICommandHandler<RemoveCareTeamMemberCommand, Unit>
{
    private readonly ICareTeamRepository _careTeams;
    private readonly IUnitOfWork _unitOfWork;
    public RemoveCareTeamMemberCommandHandler(ICareTeamRepository careTeams, IUnitOfWork unitOfWork)
    {
        _careTeams = careTeams;
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> HandleAsync(RemoveCareTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await _careTeams.GetByPatientAsync(request.PatientId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No care team for this patient.");
        team.RemoveMember(request.ProviderId);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public sealed class SetPrimaryCareTeamMemberCommandHandler : ICommandHandler<SetPrimaryCareTeamMemberCommand, Unit>
{
    private readonly ICareTeamRepository _careTeams;
    private readonly IUnitOfWork _unitOfWork;
    public SetPrimaryCareTeamMemberCommandHandler(ICareTeamRepository careTeams, IUnitOfWork unitOfWork)
    {
        _careTeams = careTeams;
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> HandleAsync(SetPrimaryCareTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var team = await _careTeams.GetByPatientAsync(request.PatientId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No care team for this patient.");
        team.SetPrimary(request.ProviderId);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
