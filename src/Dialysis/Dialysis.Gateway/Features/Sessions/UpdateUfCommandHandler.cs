using Dialysis.Persistence;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Gateway.Features.Sessions;

public sealed class UpdateUfCommandHandler : ICommandHandler<UpdateUfCommand, UpdateUfResult>
{
    private readonly DialysisDbContext _db;
    private readonly ISessionRepository _repository;
    private readonly ITenantContext _tenantContext;

    public UpdateUfCommandHandler(DialysisDbContext db, ISessionRepository repository, ITenantContext tenantContext)
    {
        _db = db;
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<UpdateUfResult> HandleAsync(UpdateUfCommand request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var sessionId = new SessionId(request.SessionId);
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id.ToString() == sessionId.Value, cancellationToken);
        if (session is null)
            return new UpdateUfResult(null);

        session.UpdateUf(request.UfRemovedKg);
        await _repository.SaveChangesAsync(cancellationToken);
        return new UpdateUfResult(session);
    }
}
