using Dialysis.Hie.Consent.Domain;
using Dialysis.Hie.Consent.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.Hie.Persistence.Repositories;

public sealed class EfConsentRepository(HieDbContext db) : IConsentRepository
{
    public async Task<ConsentRecord?> FindActiveAsync(Guid patientId, string partnerId, string scope, ConsentDirection direction, DateTime atUtc, CancellationToken cancellationToken = default)
    {
        return await db.Consents
            .Where(c => c.PatientId == patientId
                && c.PartnerId == partnerId
                && c.Scope == scope
                && c.Direction == direction
                && c.RevokedAtUtc == null
                && c.EffectiveFromUtc <= atUtc
                && (c.EffectiveToUtc == null || c.EffectiveToUtc > atUtc))
            .OrderByDescending(c => c.EffectiveFromUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ConsentRecord?> FindActiveByExternalReferenceAsync(string externalPatientReference, string partnerId, string scope, ConsentDirection direction, DateTime atUtc, CancellationToken cancellationToken = default)
    {
        // v1: inbound consents are issued against an internal patient id; partners that haven't been matched
        // yet are allowed if a wildcard consent with PatientId = Guid.Empty exists for the (partner, scope).
        return await db.Consents
            .Where(c => c.PatientId == Guid.Empty
                && c.PartnerId == partnerId
                && c.Scope == scope
                && c.Direction == direction
                && c.RevokedAtUtc == null
                && c.EffectiveFromUtc <= atUtc
                && (c.EffectiveToUtc == null || c.EffectiveToUtc > atUtc))
            .OrderByDescending(c => c.EffectiveFromUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<ConsentRecord?> GetAsync(Guid consentId, CancellationToken cancellationToken = default) =>
        db.Consents.FirstOrDefaultAsync(c => c.Id == consentId, cancellationToken);

    public Task AddAsync(ConsentRecord consent, CancellationToken cancellationToken = default) =>
        db.Consents.AddAsync(consent, cancellationToken).AsTask();

    public async Task<IReadOnlyList<ConsentRecord>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await db.Consents
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.EffectiveFromUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
