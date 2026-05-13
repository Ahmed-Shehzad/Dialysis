using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.EHR.PatientPortal.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Stores;

public sealed class PortalAppointmentRequestRepository(EhrDbContext db) : IPortalAppointmentRequestRepository
{
    public Task<PortalAppointmentRequest?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.PortalAppointmentRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PortalAppointmentRequest>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await db.PortalAppointmentRequests
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.EarliestPreferredUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(PortalAppointmentRequest request) => db.PortalAppointmentRequests.Add(request);
}

public sealed class SecureMessageRepository(EhrDbContext db) : ISecureMessageRepository
{
    public Task<SecureMessage?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.SecureMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SecureMessage>> ListByThreadAsync(Guid threadId, CancellationToken cancellationToken = default) =>
        await db.SecureMessages
            .Where(m => m.ThreadId == threadId)
            .OrderBy(m => m.SentAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(SecureMessage message) => db.SecureMessages.Add(message);
}
