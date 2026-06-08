using Dialysis.HIE.Inbound.Domain;
using Dialysis.HIE.Inbound.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

public sealed class EfPatientIndex : IPatientIndex
{
    private readonly HieDbContext _db;
    public EfPatientIndex(HieDbContext db) => _db = db;
    public async Task<PatientIndexEntry> UpsertAsync(PatientIndexEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var existing = await _db.PatientIndexEntries
            .FirstOrDefaultAsync(p => p.PartnerId == entry.PartnerId && p.ExternalLogicalId == entry.ExternalLogicalId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            existing.Refresh(entry.MedicalRecordNumber, entry.FamilyName, entry.GivenName, entry.DateOfBirth, entry.SexAtBirthCode, entry.UpdatedAtUtc);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return existing;
        }

        // Own the save (isolated) so a concurrent insert that loses the race against the unique
        // UX_PatientIndex_PartnerExternalId index resolves to the committed winner instead of
        // surfacing the violation — and the returned id stays stable across the race.
        await _db.PatientIndexEntries.AddAsync(entry, cancellationToken).ConfigureAwait(false);
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return entry;
        }
        catch (DbUpdateException)
        {
            // A concurrent ingest committed the same (PartnerId, ExternalLogicalId) between our read
            // and this save. Detach the doomed insert, re-read the winner (which must now exist),
            // refresh it with the incoming demographics, and return that stable persisted entity. If
            // the re-read can't find a winner the failure was something else and must surface.
            _db.Entry(entry).State = EntityState.Detached;
            var winner = await _db.PatientIndexEntries
                .FirstOrDefaultAsync(p => p.PartnerId == entry.PartnerId && p.ExternalLogicalId == entry.ExternalLogicalId, cancellationToken)
                .ConfigureAwait(false);
            if (winner is null)
                throw;
            winner.Refresh(entry.MedicalRecordNumber, entry.FamilyName, entry.GivenName, entry.DateOfBirth, entry.SexAtBirthCode, entry.UpdatedAtUtc);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return winner;
        }
    }

    public async Task<IReadOnlyList<PatientIndexEntry>> MatchAsync(
        string? medicalRecordNumber,
        string? familyName,
        string? givenName,
        DateOnly? dateOfBirth,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.PatientIndexEntries.AsQueryable();
        if (!string.IsNullOrWhiteSpace(medicalRecordNumber))
            query = query.Where(p => p.MedicalRecordNumber == medicalRecordNumber);
        if (!string.IsNullOrWhiteSpace(familyName))
            query = query.Where(p => p.FamilyName == familyName);
        if (!string.IsNullOrWhiteSpace(givenName))
            query = query.Where(p => p.GivenName == givenName);
        if (dateOfBirth is { } dob)
            query = query.Where(p => p.DateOfBirth == dob);
        return await query.Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PatientIndexEntry>> MatchCandidatesAsync(
        string? medicalRecordNumber,
        string? familyName,
        DateOnly? dateOfBirth,
        int take,
        CancellationToken cancellationToken = default)
    {
        var mrn = string.IsNullOrWhiteSpace(medicalRecordNumber) ? null : medicalRecordNumber.Trim();
        var family = string.IsNullOrWhiteSpace(familyName) ? null : familyName.Trim();

        // Blocking: any entry sharing an MRN, a DOB, or a family name (case-insensitive) is a
        // candidate worth scoring. Keeps the probabilistic pass off a full-table scan.
        var query = _db.PatientIndexEntries.AsNoTracking().Where(p =>
            (mrn != null && p.MedicalRecordNumber != null && EF.Functions.ILike(p.MedicalRecordNumber, mrn)) ||
            (dateOfBirth != null && p.DateOfBirth == dateOfBirth) ||
            (family != null && p.FamilyName != null && EF.Functions.ILike(p.FamilyName, family)));

        return await query.Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
