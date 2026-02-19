using System.Linq.Expressions;

using BuildingBlocks;
using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Treatment.Infrastructure.Persistence;

public sealed class TreatmentSessionRepository : Repository<TreatmentSession>, ITreatmentSessionRepository
{
    private readonly TreatmentDbContext _db;
    private readonly ITenantContext _tenant;

    public TreatmentSessionRepository(TreatmentDbContext db, ITenantContext tenant) : base(db)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<TreatmentSession?> GetBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await _db.TreatmentSessions
            .AsNoTracking()
            .Include(s => s.Observations)
            .FirstOrDefaultAsync(s => s.TenantId == new TenantId(_tenant.TenantId) && s.SessionId == sessionId, cancellationToken);
    }

    public async Task<TreatmentSession> GetOrCreateAsync(SessionId sessionId, MedicalRecordNumber? patientMrn, DeviceId? deviceId, CancellationToken cancellationToken = default)
    {
        TreatmentSession? existing = await _db.TreatmentSessions
            .Include(s => s.Observations)
            .FirstOrDefaultAsync(s => s.TenantId == new TenantId(_tenant.TenantId) && s.SessionId == sessionId, cancellationToken);

        if (existing is not null)
        {
            existing.UpdateContext(patientMrn, deviceId);
            return existing;
        }

        var session = TreatmentSession.Start(sessionId, patientMrn, deviceId, _tenant.TenantId);
        _ = _db.TreatmentSessions.Add(session);
        return session;
    }

    public async override Task AddAsync(TreatmentSession entity, CancellationToken cancellationToken = default) =>
        _ = await _db.TreatmentSessions.AddAsync(entity, cancellationToken);

    public async override Task AddAsync(IEnumerable<TreatmentSession> entities, CancellationToken cancellationToken = default) =>
        await _db.TreatmentSessions.AddRangeAsync(entities, cancellationToken);

    public async override Task<IReadOnlyList<TreatmentSession>> GetManyAsync(
        Expression<Func<TreatmentSession, bool>> expression,
        Expression<Func<TreatmentSession, object>>? orderByExpression = null,
        bool orderByDescending = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TreatmentSession> query = _db.TreatmentSessions.AsNoTracking().Where(expression);

        if (orderByExpression != null)
            query = orderByDescending ? query.OrderByDescending(orderByExpression) : query.OrderBy(orderByExpression);

        return await query.ToListAsync(cancellationToken);
    }

    public async override Task<TreatmentSession?> GetAsync(Expression<Func<TreatmentSession, bool>> expression, CancellationToken cancellationToken = default) =>
        await _db.TreatmentSessions.FirstOrDefaultAsync(expression, cancellationToken);

    public override void Update(TreatmentSession entity) => _db.Update(entity);
    public override void Update(IEnumerable<TreatmentSession> entities) => _db.UpdateRange(entities);
    public override void Delete(TreatmentSession entity) => _db.Remove(entity);
    public override void Delete(IEnumerable<TreatmentSession> entities) => _db.RemoveRange(entities);
}
