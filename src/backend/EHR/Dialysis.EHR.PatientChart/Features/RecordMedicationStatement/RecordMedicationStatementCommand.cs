using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.RecordMedicationStatement;

public sealed record RecordMedicationStatementCommand(
    Guid PatientId,
    string MedicationRxnormCode,
    string? MedicationDisplay,
    string DoseText,
    string FrequencyText,
    DateOnly StartedOn,
    string? ReasonText)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.MedicationRecord;
}
