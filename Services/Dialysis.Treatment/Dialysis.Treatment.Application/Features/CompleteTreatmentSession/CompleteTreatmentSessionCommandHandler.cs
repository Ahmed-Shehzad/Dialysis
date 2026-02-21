using BuildingBlocks.Caching;
using BuildingBlocks.Tenancy;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain;
using Dialysis.Treatment.Application.Domain.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.CompleteTreatmentSession;

internal sealed class CompleteTreatmentSessionCommandHandler : ICommandHandler<CompleteTreatmentSessionCommand, CompleteTreatmentSessionResponse>
{
    private const string TreatmentKeyPrefix = "treatment";

    private readonly ITreatmentSessionRepository _repository;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly ITenantContext _tenant;

    public CompleteTreatmentSessionCommandHandler(
        ITreatmentSessionRepository repository,
        ICacheInvalidator cacheInvalidator,
        ITenantContext tenant)
    {
        _repository = repository;
        _cacheInvalidator = cacheInvalidator;
        _tenant = tenant;
    }

    public async Task<CompleteTreatmentSessionResponse> HandleAsync(CompleteTreatmentSessionCommand request, CancellationToken cancellationToken = default)
    {
        TreatmentSession? session = await _repository.GetBySessionIdForUpdateAsync(request.SessionId, cancellationToken);
        if (session is null)
            throw new KeyNotFoundException($"Treatment session {request.SessionId.Value} not found.");

        if (session.Status != TreatmentSessionStatus.Completed)
        {
            session.Complete();
            _repository.Update(session);
            await _repository.SaveChangesAsync(cancellationToken);
            await _cacheInvalidator.InvalidateAsync($"{_tenant.TenantId}:{TreatmentKeyPrefix}:{request.SessionId.Value}", cancellationToken);
        }

        return new CompleteTreatmentSessionResponse(request.SessionId.Value, "Completed");
    }
}
