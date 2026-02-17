using Dialysis.Domain.Entities;
using Dialysis.Persistence.Abstractions;

namespace Dialysis.Persistence;

public sealed class ConditionRepository : IConditionRepository
{
    private readonly DialysisDbContext _db;

    public ConditionRepository(DialysisDbContext db) => _db = db;

    public async Task AddAsync(Condition condition, CancellationToken cancellationToken = default)
    {
        _db.Conditions.Add(condition);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Condition condition, CancellationToken cancellationToken = default)
    {
        _db.Conditions.Update(condition);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Condition condition, CancellationToken cancellationToken = default)
    {
        _db.Conditions.Remove(condition);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
