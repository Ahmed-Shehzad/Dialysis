using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterSpecialistEncounter;

public sealed record RegisterSpecialistEncounterCommand : ICommand<Guid>, IPermissionedCommand
{
    public RegisterSpecialistEncounterCommand(Guid PatientId,
        string SpecialtyCode,
        string ExternalSystemCode,
        string Summary,
        DateTime? RecordedAtUtc = null)
    {
        this.PatientId = PatientId;
        this.SpecialtyCode = SpecialtyCode;
        this.ExternalSystemCode = ExternalSystemCode;
        this.Summary = Summary;
        this.RecordedAtUtc = RecordedAtUtc;
    }
    public string RequiredPermission => HisPermissions.RaCommandsWrite;
    public Guid PatientId { get; init; }
    public string SpecialtyCode { get; init; }
    public string ExternalSystemCode { get; init; }
    public string Summary { get; init; }
    public DateTime? RecordedAtUtc { get; init; }
    public void Deconstruct(out Guid PatientId, out string SpecialtyCode, out string ExternalSystemCode, out string Summary, out DateTime? RecordedAtUtc)
    {
        PatientId = this.PatientId;
        SpecialtyCode = this.SpecialtyCode;
        ExternalSystemCode = this.ExternalSystemCode;
        Summary = this.Summary;
        RecordedAtUtc = this.RecordedAtUtc;
    }
}
