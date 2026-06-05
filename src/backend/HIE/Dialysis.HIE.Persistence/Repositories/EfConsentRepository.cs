using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Consent.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

public sealed class EfConsentRepository : IConsentRepository
{
    private readonly HieDbContext _db;
    public EfConsentRepository(HieDbContext db) => _db = db;
    public async Task<ConsentRecord?> FindActiveAsync(Guid patientId, string partnerId, string scope, ConsentDirection direction, DateTime atUtc, string? purpose = null, CancellationToken cancellationToken = default)
    {
        return await _db.Consents
            .Where(c => c.PatientId == patientId
                && c.PartnerId == partnerId
                && c.Scope == scope
                && c.Direction == direction
                // A null Purpose on the record is a wildcard that honours any requested purpose;
                // a specific purpose only matches a request declaring that same purpose.
                && (c.Purpose == null || purpose == null || c.Purpose == purpose)
                && c.RevokedAtUtc == null
                && c.EffectiveFromUtc <= atUtc
                && (c.EffectiveToUtc == null || c.EffectiveToUtc > atUtc))
            .OrderByDescending(c => c.EffectiveFromUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ConsentRecord?> FindActiveByExternalReferenceAsync(string externalPatientReference, string partnerId, string scope, ConsentDirection direction, DateTime atUtc, string? purpose = null, CancellationToken cancellationToken = default)
    {
        // v1: inbound consents are issued against an internal patient id; partners that haven't been matched
        // yet are allowed if a wildcard consent with PatientId = Guid.Empty exists for the (partner, scope).
        return await _db.Consents
            .Where(c => c.PatientId == Guid.Empty
                && c.PartnerId == partnerId
                && c.Scope == scope
                && c.Direction == direction
                && (c.Purpose == null || purpose == null || c.Purpose == purpose)
                && c.RevokedAtUtc == null
                && c.EffectiveFromUtc <= atUtc
                && (c.EffectiveToUtc == null || c.EffectiveToUtc > atUtc))
            .OrderByDescending(c => c.EffectiveFromUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<ConsentRecord?> GetAsync(Guid consentId, CancellationToken cancellationToken = default) =>
        _db.Consents.FirstOrDefaultAsync(c => c.Id == consentId, cancellationToken);

    public Task AddAsync(ConsentRecord consent, CancellationToken cancellationToken = default) =>
        _db.Consents.AddAsync(consent, cancellationToken).AsTask();

    public async Task<IReadOnlyList<ConsentRecord>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await _db.Consents
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.EffectiveFromUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
