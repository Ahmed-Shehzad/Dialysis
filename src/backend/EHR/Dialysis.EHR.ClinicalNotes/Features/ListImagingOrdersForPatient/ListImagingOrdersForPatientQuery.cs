using Dialysis.CQRS.Queries;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.ClinicalNotes.Features.ListImagingOrdersForPatient;

/// <summary>Lists a patient's imaging orders (most-recent first) for the chart imaging panel.</summary>
public sealed record ListImagingOrdersForPatientQuery(Guid PatientId, int Take = 50)
    : IQuery<IReadOnlyList<ImagingOrderDto>>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.ImagingStudyRead;
}

/// <summary>Imaging order projection for the chart panel.</summary>
public sealed record ImagingOrderDto(
    Guid Id,
    Guid PatientId,
    string AccessionNumber,
    string ModalityCode,
    string BodySiteCode,
    string? ReasonText,
    ImagingOrderStatus Status,
    string? StudyInstanceUid);

public sealed class ListImagingOrdersForPatientQueryHandler
    : IQueryHandler<ListImagingOrdersForPatientQuery, IReadOnlyList<ImagingOrderDto>>
{
    private readonly IImagingOrderRepository _imagingOrders;
    public ListImagingOrdersForPatientQueryHandler(IImagingOrderRepository imagingOrders) =>
        _imagingOrders = imagingOrders;

    public async Task<IReadOnlyList<ImagingOrderDto>> HandleAsync(
        ListImagingOrdersForPatientQuery request, CancellationToken cancellationToken)
    {
        var take = request.Take is > 0 and <= 200 ? request.Take : 50;
        var orders = await _imagingOrders
            .ListByPatientAsync(request.PatientId, take, cancellationToken)
            .ConfigureAwait(false);

        return [.. orders.Select(o => new ImagingOrderDto(
            o.Id, o.PatientId, o.AccessionNumber, o.ModalityCode, o.BodySiteCode, o.ReasonText,
            o.Status, o.StudyInstanceUid))];
    }
}
