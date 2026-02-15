using Intercessor.Abstractions;

namespace Dialysis.IdentityAdmission.Features.SessionScheduling;

public sealed record CreateSessionCommand : ICommand<CreateSessionResult>
{
    public required string PatientId { get; init; }
    public required string DeviceId { get; init; }
    public required DateTimeOffset ScheduledStart { get; init; }
}
