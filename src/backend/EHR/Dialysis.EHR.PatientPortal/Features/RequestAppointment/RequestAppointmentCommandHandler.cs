using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.EHR.PatientPortal.Ports;

namespace Dialysis.EHR.PatientPortal.Features.RequestAppointment;

public sealed class RequestAppointmentCommandHandler : ICommandHandler<RequestAppointmentCommand, Guid>
{
    private readonly IPortalAppointmentRequestRepository _requests;
    private readonly IUnitOfWork _unitOfWork;
    public RequestAppointmentCommandHandler(IPortalAppointmentRequestRepository requests,
        IUnitOfWork unitOfWork)
    {
        _requests = requests;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RequestAppointmentCommand request, CancellationToken cancellationToken)
    {
        // Idempotent submit: filing the same request again (same reason + preferred window) while an
        // earlier one is still Pending must not stack a duplicate row on the staff worklist — return the
        // existing request. Retries, double-taps, and the dev data-simulator would otherwise pile up
        // identical requests, which then collide on approval (each books the same slot -> 409). Match the
        // trimmed reason the aggregate persists; a blank reason falls through to Submit's validation.
        var reasonText = request.ReasonText?.Trim() ?? string.Empty;
        if (reasonText.Length > 0)
        {
            var existing = await _requests.FindOpenDuplicateAsync(
                request.PatientId, reasonText, request.EarliestPreferredUtc, request.LatestPreferredUtc, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
                return existing.Id;
        }

        var id = Guid.CreateVersion7();
        // Pass the non-null local (Submit re-validates + trims; a blank reason still throws "Reason required").
        var portalRequest = PortalAppointmentRequest.Submit(
            id,
            request.PatientId,
            reasonText,
            request.EarliestPreferredUtc,
            request.LatestPreferredUtc);
        _requests.Add(portalRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
