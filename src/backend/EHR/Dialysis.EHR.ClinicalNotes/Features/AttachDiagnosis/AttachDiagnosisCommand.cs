using Dialysis.CQRS.Commands;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.AttachDiagnosis;

public sealed record AttachDiagnosisCommand : ICommand, IPermissionedCommand
{
    public AttachDiagnosisCommand(Guid EncounterId, string Icd10Code, string? Display, DiagnosisRank Rank)
    {
        this.EncounterId = EncounterId;
        this.Icd10Code = Icd10Code;
        this.Display = Display;
        this.Rank = Rank;
    }
    public string RequiredPermission => EhrPermissions.DiagnosisAttach;
    public Guid EncounterId { get; init; }
    public string Icd10Code { get; init; }
    public string? Display { get; init; }
    public DiagnosisRank Rank { get; init; }
    public void Deconstruct(out Guid EncounterId, out string Icd10Code, out string? Display, out DiagnosisRank Rank)
    {
        EncounterId = this.EncounterId;
        Icd10Code = this.Icd10Code;
        Display = this.Display;
        Rank = this.Rank;
    }
}
