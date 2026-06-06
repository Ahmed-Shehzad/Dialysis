using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.EHR.PatientPortal.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Stores;

public sealed class PortalAppointmentRequestRepository : IPortalAppointmentRequestRepository
{
    private readonly EhrDbContext _db;
    public PortalAppointmentRequestRepository(EhrDbContext db) => _db = db;
    public Task<PortalAppointmentRequest?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.PortalAppointmentRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PortalAppointmentRequest>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await _db.PortalAppointmentRequests
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.EarliestPreferredUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<PortalAppointmentRequest>> ListByStatusAsync(PortalAppointmentRequestStatus status, int take, CancellationToken cancellationToken = default) =>
        await _db.PortalAppointmentRequests
            .Where(r => r.Status == status)
            .OrderBy(r => r.EarliestPreferredUtc)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(PortalAppointmentRequest request) => _db.PortalAppointmentRequests.Add(request);
}

public sealed class SecureMessageRepository : ISecureMessageRepository
{
    private readonly EhrDbContext _db;
    public SecureMessageRepository(EhrDbContext db) => _db = db;
    public Task<SecureMessage?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.SecureMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SecureMessage>> ListByThreadAsync(Guid threadId, CancellationToken cancellationToken = default) =>
        await _db.SecureMessages
            .Where(m => m.ThreadId == threadId)
            .OrderBy(m => m.SentAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<SecureMessage>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await _db.SecureMessages
            .Where(m => m.PatientId == patientId)
            .OrderBy(m => m.SentAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(SecureMessage message) => _db.SecureMessages.Add(message);
}
