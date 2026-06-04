using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.RecordMedicationStatement;

public sealed record RecordMedicationStatementCommand : ICommand<Guid>, IPermissionedCommand
{
    public RecordMedicationStatementCommand(Guid PatientId,
        string MedicationRxnormCode,
        string? MedicationDisplay,
        string DoseText,
        string FrequencyText,
        DateOnly StartedOn,
        string? ReasonText)
    {
        this.PatientId = PatientId;
        this.MedicationRxnormCode = MedicationRxnormCode;
        this.MedicationDisplay = MedicationDisplay;
        this.DoseText = DoseText;
        this.FrequencyText = FrequencyText;
        this.StartedOn = StartedOn;
        this.ReasonText = ReasonText;
    }
    public string RequiredPermission => EhrPermissions.MedicationRecord;
    public Guid PatientId { get; init; }
    public string MedicationRxnormCode { get; init; }
    public string? MedicationDisplay { get; init; }
    public string DoseText { get; init; }
    public string FrequencyText { get; init; }
    public DateOnly StartedOn { get; init; }
    public string? ReasonText { get; init; }
    public void Deconstruct(out Guid PatientId, out string MedicationRxnormCode, out string? MedicationDisplay, out string DoseText, out string FrequencyText, out DateOnly StartedOn, out string? ReasonText)
    {
        PatientId = this.PatientId;
        MedicationRxnormCode = this.MedicationRxnormCode;
        MedicationDisplay = this.MedicationDisplay;
        DoseText = this.DoseText;
        FrequencyText = this.FrequencyText;
        StartedOn = this.StartedOn;
        ReasonText = this.ReasonText;
    }
}
