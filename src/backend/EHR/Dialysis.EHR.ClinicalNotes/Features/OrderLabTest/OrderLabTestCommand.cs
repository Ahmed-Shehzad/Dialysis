using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderLabTest;

public sealed record OrderLabTestCommand(
    Guid PatientId,
    Guid EncounterId,
    Guid OrderingProviderId,
    string LabFacilityCode,
    IReadOnlyList<string> LoincPanelCodes,
    string TransmissionFormat = EhrLabFormats.FhirServiceRequest)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => EhrPermissions.LabOrder;
}
