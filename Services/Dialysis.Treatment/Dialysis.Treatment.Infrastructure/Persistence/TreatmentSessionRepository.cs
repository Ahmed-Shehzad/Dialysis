using BuildingBlocks.ValueObjects;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Domain;
using Dialysis.Treatment.Application.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Treatment.Infrastructure.Persistence;

public sealed class TreatmentSessionRepository : ITreatmentSessionRepository
{
    private readonly TreatmentDbContext _db;

    public TreatmentSessionRepository(TreatmentDbContext db)
    {
        _db = db;
    }

    public async Task<TreatmentSession?> GetBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await _db.TreatmentSessions
            .AsNoTracking()
            .Include(s => s.Observations)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);
    }

    public async Task<TreatmentSession> GetOrCreateAsync(SessionId sessionId, MedicalRecordNumber? patientMrn, DeviceId? deviceId, CancellationToken cancellationToken = default)
    {
        var existing = await _db.TreatmentSessions
            .Include(s => s.Observations)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);

        if (existing is not null)
        {
            existing.UpdateContext(patientMrn, deviceId);
            return existing;
        }

        var session = TreatmentSession.Start(sessionId, patientMrn, deviceId);
        _ = _db.TreatmentSessions.Add(session);
        return session;
    }

    public async Task SaveAsync(TreatmentSession session, CancellationToken cancellationToken = default)
    {
        _ = await _db.SaveChangesAsync(cancellationToken);
    }
}
