using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Scheduling.Features.BookAppointment;

public sealed record BookAppointmentCommand : ICommand<Guid>, IPermissionedCommand
{
    public BookAppointmentCommand(Guid PatientId,
        Guid ProviderId,
        DateTime SlotStartUtc,
        DateTime SlotEndUtc)
    {
        this.PatientId = PatientId;
        this.ProviderId = ProviderId;
        this.SlotStartUtc = SlotStartUtc;
        this.SlotEndUtc = SlotEndUtc;
    }
    public string RequiredPermission => HisPermissions.SchedulingBook;
    public Guid PatientId { get; init; }
    public Guid ProviderId { get; init; }
    public DateTime SlotStartUtc { get; init; }
    public DateTime SlotEndUtc { get; init; }
    public void Deconstruct(out Guid PatientId, out Guid ProviderId, out DateTime SlotStartUtc, out DateTime SlotEndUtc)
    {
        PatientId = this.PatientId;
        ProviderId = this.ProviderId;
        SlotStartUtc = this.SlotStartUtc;
        SlotEndUtc = this.SlotEndUtc;
    }
}
