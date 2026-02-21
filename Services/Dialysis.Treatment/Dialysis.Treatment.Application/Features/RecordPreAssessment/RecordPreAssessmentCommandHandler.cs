using BuildingBlocks.Caching;
using BuildingBlocks.Tenancy;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.RecordPreAssessment;

internal sealed class RecordPreAssessmentCommandHandler : ICommandHandler<RecordPreAssessmentCommand, RecordPreAssessmentResponse>
{
    private const string TreatmentKeyPrefix = "treatment";

    private readonly IPreAssessmentRepository _preAssessmentRepository;
    private readonly ITreatmentSessionRepository _sessionRepository;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly ITenantContext _tenant;

    public RecordPreAssessmentCommandHandler(
        IPreAssessmentRepository preAssessmentRepository,
        ITreatmentSessionRepository sessionRepository,
        ICacheInvalidator cacheInvalidator,
        ITenantContext tenant)
    {
        _preAssessmentRepository = preAssessmentRepository;
        _sessionRepository = sessionRepository;
        _cacheInvalidator = cacheInvalidator;
        _tenant = tenant;
    }

    public async Task<RecordPreAssessmentResponse> HandleAsync(RecordPreAssessmentCommand request, CancellationToken cancellationToken = default)
    {
        _ = await _sessionRepository.GetBySessionIdAsync(request.SessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Treatment session {request.SessionId.Value} not found.");

        PreAssessment? existing = await _preAssessmentRepository.GetBySessionIdAsync(request.SessionId, cancellationToken);

        if (existing is not null)
        {
            existing.Update(
                request.PreWeightKg,
                request.BpSystolic,
                request.BpDiastolic,
                request.AccessTypeValue,
                request.PrescriptionConfirmed,
                request.PainSymptomNotes,
                request.RecordedBy);
            _preAssessmentRepository.Update(existing);
        }
        else
        {
            var preAssessment = PreAssessment.Create(
                request.SessionId,
                _tenant.TenantId,
                request.PreWeightKg,
                request.BpSystolic,
                request.BpDiastolic,
                request.AccessTypeValue,
                request.PrescriptionConfirmed,
                request.PainSymptomNotes,
                request.RecordedBy);
            await _preAssessmentRepository.AddAsync(preAssessment, cancellationToken);
        }

        await _preAssessmentRepository.SaveChangesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync($"{_tenant.TenantId}:{TreatmentKeyPrefix}:{request.SessionId.Value}", cancellationToken);

        PreAssessment? saved = await _preAssessmentRepository.GetBySessionIdAsync(request.SessionId, cancellationToken);
        return new RecordPreAssessmentResponse(request.SessionId.Value, saved!.RecordedAt);
    }
}
