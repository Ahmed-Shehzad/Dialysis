using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.Integration.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.Integration.Features.MarkHospitalEventFollowedUp;

/// <summary>Marks a hospital event as followed-up so it drops off the care-coordination worklist.</summary>
public sealed record MarkHospitalEventFollowedUpCommand(Guid HospitalEventId)
    : ICommand<Unit>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.CareCoordinationFollowUp;
}

public sealed class MarkHospitalEventFollowedUpCommandHandler : ICommandHandler<MarkHospitalEventFollowedUpCommand, Unit>
{
    private readonly IHospitalEventRepository _events;
    public MarkHospitalEventFollowedUpCommandHandler(IHospitalEventRepository events) => _events = events;

    public async Task<Unit> HandleAsync(MarkHospitalEventFollowedUpCommand request, CancellationToken cancellationToken)
    {
        var ok = await _events.MarkFollowedUpAsync(request.HospitalEventId, DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
        if (!ok)
            throw new InvalidOperationException($"Hospital event '{request.HospitalEventId}' not found.");
        return Unit.Value;
    }
}
