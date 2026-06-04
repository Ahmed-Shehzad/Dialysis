using Dialysis.BuildingBlocks.DurableCommandBus;
using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientChart.Features.RecordAllergy;

/// <summary>
/// Allergy recording — clinically critical write. Opted into the durable command bus
/// the same way as <c>RecordVitalSignCommand</c>; flag
/// <c>Ehr:DurableCommands:RecordAllergy:Enabled</c> controls whether the controller
/// returns 202 or stays on the synchronous path.
/// </summary>
[DurableCommand("ehr")]
public sealed record RecordAllergyCommand : ICommand<Guid>, IPermissionedCommand
{
    /// <summary>
    /// Allergy recording — clinically critical write. Opted into the durable command bus
    /// the same way as <c>RecordVitalSignCommand</c>; flag
    /// <c>Ehr:DurableCommands:RecordAllergy:Enabled</c> controls whether the controller
    /// returns 202 or stays on the synchronous path.
    /// </summary>
    public RecordAllergyCommand(Guid PatientId,
        string AllergenSystem,
        string AllergenCode,
        string? AllergenDisplay,
        AllergySeverity Severity,
        AllergyVerificationStatus VerificationStatus,
        string? ReactionText,
        DateOnly? OnsetDate,
        Guid AllergyId = default)
    {
        this.PatientId = PatientId;
        this.AllergenSystem = AllergenSystem;
        this.AllergenCode = AllergenCode;
        this.AllergenDisplay = AllergenDisplay;
        this.Severity = Severity;
        this.VerificationStatus = VerificationStatus;
        this.ReactionText = ReactionText;
        this.OnsetDate = OnsetDate;
        this.AllergyId = AllergyId;
    }
    public string RequiredPermission => EhrPermissions.AllergyRecord;
    public Guid PatientId { get; init; }
    public string AllergenSystem { get; init; }
    public string AllergenCode { get; init; }
    public string? AllergenDisplay { get; init; }
    public AllergySeverity Severity { get; init; }
    public AllergyVerificationStatus VerificationStatus { get; init; }
    public string? ReactionText { get; init; }
    public DateOnly? OnsetDate { get; init; }
    public Guid AllergyId { get; init; }
    public void Deconstruct(out Guid PatientId, out string AllergenSystem, out string AllergenCode, out string? AllergenDisplay, out AllergySeverity Severity, out AllergyVerificationStatus VerificationStatus, out string? ReactionText, out DateOnly? OnsetDate, out Guid AllergyId)
    {
        PatientId = this.PatientId;
        AllergenSystem = this.AllergenSystem;
        AllergenCode = this.AllergenCode;
        AllergenDisplay = this.AllergenDisplay;
        Severity = this.Severity;
        VerificationStatus = this.VerificationStatus;
        ReactionText = this.ReactionText;
        OnsetDate = this.OnsetDate;
        AllergyId = this.AllergyId;
    }
}
