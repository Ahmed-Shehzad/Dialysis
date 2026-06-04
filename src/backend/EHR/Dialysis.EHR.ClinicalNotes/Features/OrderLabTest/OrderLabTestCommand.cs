using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderLabTest;

public sealed record OrderLabTestCommand : ICommand<Guid>, IPermissionedCommand
{
    public OrderLabTestCommand(Guid PatientId,
        Guid EncounterId,
        Guid OrderingProviderId,
        string LabFacilityCode,
        IReadOnlyList<string> LoincPanelCodes,
        string TransmissionFormat = EhrLabFormats.FhirServiceRequest)
    {
        this.PatientId = PatientId;
        this.EncounterId = EncounterId;
        this.OrderingProviderId = OrderingProviderId;
        this.LabFacilityCode = LabFacilityCode;
        this.LoincPanelCodes = LoincPanelCodes;
        this.TransmissionFormat = TransmissionFormat;
    }
    public string RequiredPermission => EhrPermissions.LabOrder;
    public Guid PatientId { get; init; }
    public Guid EncounterId { get; init; }
    public Guid OrderingProviderId { get; init; }
    public string LabFacilityCode { get; init; }
    public IReadOnlyList<string> LoincPanelCodes { get; init; }
    public string TransmissionFormat { get; init; }
    public void Deconstruct(out Guid PatientId, out Guid EncounterId, out Guid OrderingProviderId, out string LabFacilityCode, out IReadOnlyList<string> LoincPanelCodes, out string TransmissionFormat)
    {
        PatientId = this.PatientId;
        EncounterId = this.EncounterId;
        OrderingProviderId = this.OrderingProviderId;
        LabFacilityCode = this.LabFacilityCode;
        LoincPanelCodes = this.LoincPanelCodes;
        TransmissionFormat = this.TransmissionFormat;
    }
}
