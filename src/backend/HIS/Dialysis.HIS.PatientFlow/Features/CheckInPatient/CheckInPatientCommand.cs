using Dialysis.CQRS.Commands;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.PatientFlow.Features.CheckInPatient;

/// <summary>
/// Moves a Queue entry from Expected to Waiting.
/// <c>ArrivalTimeUtc</c> records when the patient walked in (defaults to "now" client-side);
/// <c>EligibilityAcknowledged</c> is the receptionist's explicit confirmation that insurance
/// was checked at the counter — only meaningful for entries not pre-verified.
/// </summary>
public sealed record CheckInPatientCommand : ICommand<Guid>, IPermissionedCommand
{
    /// <summary>
    /// Moves a Queue entry from Expected to Waiting.
    /// <c>ArrivalTimeUtc</c> records when the patient walked in (defaults to "now" client-side);
    /// <c>EligibilityAcknowledged</c> is the receptionist's explicit confirmation that insurance
    /// was checked at the counter — only meaningful for entries not pre-verified.
    /// </summary>
    public CheckInPatientCommand(Guid EntryId,
        DateTime ArrivalTimeUtc,
        bool EligibilityAcknowledged)
    {
        this.EntryId = EntryId;
        this.ArrivalTimeUtc = ArrivalTimeUtc;
        this.EligibilityAcknowledged = EligibilityAcknowledged;
    }
    public string RequiredPermission => HisPermissions.PatientFlowQueueManage;
    public Guid EntryId { get; init; }
    public DateTime ArrivalTimeUtc { get; init; }
    public bool EligibilityAcknowledged { get; init; }
    public void Deconstruct(out Guid EntryId, out DateTime ArrivalTimeUtc, out bool EligibilityAcknowledged)
    {
        EntryId = this.EntryId;
        ArrivalTimeUtc = this.ArrivalTimeUtc;
        EligibilityAcknowledged = this.EligibilityAcknowledged;
    }
}
