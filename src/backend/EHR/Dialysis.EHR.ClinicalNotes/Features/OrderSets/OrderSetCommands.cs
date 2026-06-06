using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.EHR.ClinicalNotes.SafetyChecks;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderSets;

/// <summary>A lab line in a create-order-set request.</summary>
public sealed record OrderSetLabLineDto(string LabFacilityCode, IReadOnlyList<string> LoincPanelCodes);

/// <summary>A medication line in a create-order-set request.</summary>
public sealed record OrderSetMedicationLineDto(
    string MedicationRxnormCode, string MedicationDisplay, string DoseText, string FrequencyText,
    int QuantityDispensed, int RefillsAuthorized, string PharmacyNcpdpId);

/// <summary>An imaging line in a create-order-set request.</summary>
public sealed record OrderSetImagingLineDto(string ModalityCode, string BodySiteCode, string? ReasonText);

/// <summary>Creates a reusable order set (name + lab / medication / imaging lines). Returns the set id.</summary>
public sealed record CreateOrderSetCommand(
    string Name,
    string? Description,
    IReadOnlyList<OrderSetLabLineDto> LabLines,
    IReadOnlyList<OrderSetMedicationLineDto> MedicationLines,
    IReadOnlyList<OrderSetImagingLineDto> ImagingLines) : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.OrderSetManage;
}

/// <summary>Deactivates an order set so it no longer appears in the picker.</summary>
public sealed record DeactivateOrderSetCommand(Guid OrderSetId) : ICommand<Unit>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.OrderSetManage;
}

/// <summary>
/// Applies an order set to a patient/encounter, fanning out to the individual order commands so each
/// line runs the point-of-care safety checks. <see cref="OverriddenBy"/> is server-populated.
/// </summary>
public sealed record ApplyOrderSetCommand(
    Guid OrderSetId,
    Guid PatientId,
    Guid EncounterId,
    Guid OrderingProviderId,
    bool AcknowledgeAdvisories = false,
    string? OverrideReason = null,
    string? OverriddenBy = null) : ICommand<ApplyOrderSetResult>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.OrderSetApply;
}

/// <summary>One order created by applying an order set.</summary>
public sealed record AppliedOrder(string Kind, Guid OrderId);

/// <summary>Result of applying an order set: the created orders + any non-blocking advisories raised.</summary>
public sealed record ApplyOrderSetResult(IReadOnlyList<AppliedOrder> Orders, IReadOnlyList<SafetyAdvisory> Advisories);
