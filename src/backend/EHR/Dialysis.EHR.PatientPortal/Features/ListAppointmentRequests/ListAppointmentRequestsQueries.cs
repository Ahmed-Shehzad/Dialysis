using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.EHR.PatientPortal.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientPortal.Features.ListAppointmentRequests;

/// <summary>An appointment request projected for the patient list and the staff worklist.</summary>
public sealed record AppointmentRequestView(
    Guid Id,
    Guid PatientId,
    string ReasonText,
    DateTime EarliestPreferredUtc,
    DateTime LatestPreferredUtc,
    string Status,
    Guid? CreatedAppointmentId,
    string? StaffNote);

internal static class AppointmentRequestMapping
{
    public static AppointmentRequestView ToView(this PortalAppointmentRequest r) =>
        new(r.Id, r.PatientId, r.ReasonText, r.EarliestPreferredUtc, r.LatestPreferredUtc,
            r.Status.ToString(), r.CreatedAppointmentId, r.StaffNote);
}

/// <summary>A patient's own appointment requests (the portal "my requests" list).</summary>
public sealed record ListMyAppointmentRequestsQuery(Guid PatientId)
    : IQuery<IReadOnlyList<AppointmentRequestView>>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PortalAppointmentRead;
}

public sealed class ListMyAppointmentRequestsQueryHandler
    : IQueryHandler<ListMyAppointmentRequestsQuery, IReadOnlyList<AppointmentRequestView>>
{
    private readonly IPortalAppointmentRequestRepository _requests;
    public ListMyAppointmentRequestsQueryHandler(IPortalAppointmentRequestRepository requests) => _requests = requests;

    public async Task<IReadOnlyList<AppointmentRequestView>> HandleAsync(
        ListMyAppointmentRequestsQuery request, CancellationToken cancellationToken)
    {
        var rows = await _requests.ListByPatientAsync(request.PatientId, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => r.ToView())];
    }
}

/// <summary>The staff worklist of still-pending appointment requests.</summary>
public sealed record ListPendingAppointmentRequestsQuery(int Take = 100)
    : IQuery<IReadOnlyList<AppointmentRequestView>>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PortalAppointmentManage;
}

public sealed class ListPendingAppointmentRequestsQueryHandler
    : IQueryHandler<ListPendingAppointmentRequestsQuery, IReadOnlyList<AppointmentRequestView>>
{
    private readonly IPortalAppointmentRequestRepository _requests;
    public ListPendingAppointmentRequestsQueryHandler(IPortalAppointmentRequestRepository requests) => _requests = requests;

    public async Task<IReadOnlyList<AppointmentRequestView>> HandleAsync(
        ListPendingAppointmentRequestsQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 500);
        var rows = await _requests.ListByStatusAsync(PortalAppointmentRequestStatus.Pending, take, cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => r.ToView())];
    }
}
