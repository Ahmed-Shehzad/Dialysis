using Dialysis.CQRS.Commands;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.AttachDiagnosis;

public sealed record AttachDiagnosisCommand(Guid EncounterId, string Icd10Code, string? Display, DiagnosisRank Rank)
    : ICommand, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.DiagnosisAttach;
}
