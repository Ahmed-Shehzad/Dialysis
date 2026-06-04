using Dialysis.HIE.Tefca.Domain;
using Dialysis.HIE.Tefca.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

public sealed class EfQhinPartnerRepository : IQhinPartnerRepository
{
    private readonly HieDbContext _db;
    public EfQhinPartnerRepository(HieDbContext db) => _db = db;
    public void Add(QhinPartner partner)
    {
        ArgumentNullException.ThrowIfNull(partner);
        _db.QhinPartners.Add(partner);
    }

    public void Remove(QhinPartner partner)
    {
        ArgumentNullException.ThrowIfNull(partner);
        _db.QhinPartners.Remove(partner);
    }

    public Task<QhinPartner?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        _db.QhinPartners
            .Include(p => p.TrustAnchors)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<QhinPartner>> ListAsync(CancellationToken cancellationToken) =>
        await _db.QhinPartners
            .AsNoTracking()
            .Include(p => p.TrustAnchors)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
