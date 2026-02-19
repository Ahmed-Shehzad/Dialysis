using Dialysis.Prescription.Application.Abstractions;
using Dialysis.Prescription.Application.Domain;
using Dialysis.Prescription.Application.Services;
using Dialysis.Prescription.Infrastructure.Persistence;
using Dialysis.Prescription.Infrastructure.ReadModels;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Prescription.Infrastructure;

public sealed class PrescriptionReadStore : IPrescriptionReadStore
{
    private readonly PrescriptionReadDbContext _db;

    public PrescriptionReadStore(PrescriptionReadDbContext db)
    {
        _db = db;
    }

    public async Task<PrescriptionReadDto?> GetLatestByMrnAsync(string tenantId, string mrn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mrn)) return null;
        PrescriptionReadModel? m = await _db.Prescriptions
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.PatientMrn == mrn)
            .OrderByDescending(p => p.ReceivedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return m is null ? null : ToDto(m);
    }

    public async Task<IReadOnlyList<PrescriptionReadDto>> GetAllForTenantAsync(string tenantId, int limit, CancellationToken cancellationToken = default)
    {
        List<PrescriptionReadModel> list = await _db.Prescriptions
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.ReceivedAt)
            .Take(Math.Max(1, Math.Min(limit, 10_000)))
            .ToListAsync(cancellationToken);
        return list.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<PrescriptionReadDto>> GetByPatientMrnAsync(string tenantId, string mrn, int limit, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mrn)) return [];
        List<PrescriptionReadModel> list = await _db.Prescriptions
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.PatientMrn == mrn)
            .OrderByDescending(p => p.ReceivedAt)
            .Take(Math.Max(1, Math.Min(limit, 10_000)))
            .ToListAsync(cancellationToken);
        return list.Select(ToDto).ToList();
    }

    private static PrescriptionReadDto ToDto(PrescriptionReadModel m)
    {
        List<ProfileSetting> settings = PrescriptionSettingsSerializer.FromJson(m.SettingsJson);
        decimal? bloodFlow = PrescriptionSettingResolver.GetBloodFlowRateMlMin(settings);
        decimal? ufRate = PrescriptionSettingResolver.GetUfRateMlH(settings);
        decimal? ufTarget = PrescriptionSettingResolver.GetUfTargetVolumeMl(settings);
        return new PrescriptionReadDto(
            m.OrderId,
            m.PatientMrn,
            m.Modality,
            m.OrderingProvider,
            bloodFlow,
            ufRate,
            ufTarget,
            m.ReceivedAt);
    }
}
