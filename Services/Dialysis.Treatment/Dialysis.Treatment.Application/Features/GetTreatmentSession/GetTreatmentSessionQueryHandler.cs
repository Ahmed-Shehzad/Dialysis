using Dialysis.Treatment.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.GetTreatmentSession;

internal sealed class GetTreatmentSessionQueryHandler : IQueryHandler<GetTreatmentSessionQuery, GetTreatmentSessionResponse?>
{
    private readonly ITreatmentSessionRepository _repository;

    public GetTreatmentSessionQueryHandler(ITreatmentSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetTreatmentSessionResponse?> HandleAsync(GetTreatmentSessionQuery request, CancellationToken cancellationToken = default)
    {
        var session = await _repository.GetBySessionIdAsync(request.SessionId, cancellationToken);
        if (session is null)
            return null;

        return new GetTreatmentSessionResponse(
            session.SessionId,
            session.PatientMrn?.Value,
            session.DeviceId?.Value,
            session.Status,
            session.StartedAt);
    }
}
