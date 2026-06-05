using Dialysis.CQRS.Commands;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.OrderImagingStudy;

/// <summary>
/// Orders an imaging study for a patient on an encounter. The modality (PACS/RIS via SmartConnect
/// DICOM) fulfils it; the returned study is correlated back by the generated accession number.
/// </summary>
public sealed record OrderImagingStudyCommand(
    Guid PatientId,
    Guid EncounterId,
    Guid OrderingProviderId,
    string ModalityCode,
    string BodySiteCode,
    string? ReasonText) : ICommand<Guid>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.ImagingOrder;
}
