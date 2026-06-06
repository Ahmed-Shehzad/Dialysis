using Dialysis.CQRS.Commands;
using Dialysis.EHR.ClinicalNotes.SafetyChecks;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderLabTest;

public sealed record OrderLabTestCommand : ICommand<OrderPlacementResult>, IPermissionedCommand
{
    public OrderLabTestCommand(Guid PatientId,
        Guid EncounterId,
        Guid OrderingProviderId,
        string LabFacilityCode,
        IReadOnlyList<string> LoincPanelCodes,
        string TransmissionFormat = EhrLabFormats.FhirServiceRequest,
        bool AcknowledgeAdvisories = false,
        string? OverrideReason = null,
        string? OverriddenBy = null)
    {
        this.PatientId = PatientId;
        this.EncounterId = EncounterId;
        this.OrderingProviderId = OrderingProviderId;
        this.LabFacilityCode = LabFacilityCode;
        this.LoincPanelCodes = LoincPanelCodes;
        this.TransmissionFormat = TransmissionFormat;
        this.AcknowledgeAdvisories = AcknowledgeAdvisories;
        this.OverrideReason = OverrideReason;
        this.OverriddenBy = OverriddenBy;
    }
    public string RequiredPermission => EhrPermissions.LabOrder;
    public Guid PatientId { get; init; }
    public Guid EncounterId { get; init; }
    public Guid OrderingProviderId { get; init; }
    public string LabFacilityCode { get; init; }
    public IReadOnlyList<string> LoincPanelCodes { get; init; }
    public string TransmissionFormat { get; init; }

    /// <summary>When true, blocking safety advisories are overridden (requires <see cref="OverrideReason"/>).</summary>
    public bool AcknowledgeAdvisories { get; init; }

    /// <summary>The clinician's reason for overriding a blocking advisory; audited on the lab order.</summary>
    public string? OverrideReason { get; init; }

    /// <summary>Server-populated identity of the overriding clinician (from the authenticated principal).</summary>
    public string? OverriddenBy { get; init; }
}
