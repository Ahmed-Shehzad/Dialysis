using Dialysis.Treatment.Application.Domain;

namespace Dialysis.Treatment.Application.Abstractions;

public interface IPreAssessmentRepository
{
    Task<PreAssessment?> GetBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default);
    Task AddAsync(PreAssessment entity, CancellationToken cancellationToken = default);
    void Update(PreAssessment entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
