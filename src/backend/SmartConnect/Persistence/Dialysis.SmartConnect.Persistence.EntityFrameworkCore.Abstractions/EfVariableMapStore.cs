using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public sealed class EfVariableMapStore : IVariableMapStore
{
    private readonly SmartConnectDbContext _db;
    public EfVariableMapStore(SmartConnectDbContext db) => _db = db;
    public async Task<string?> GetAsync(VariableMapScope scope, Guid? flowId, string key, CancellationToken cancellationToken = default)
    {
        var fid = flowId ?? Guid.Empty;
        return await _db.VariableMapEntries
            .Where(e => e.Scope == (int)scope && e.FlowId == fid && e.Key == key)
            .Select(e => e.Value)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SetAsync(VariableMapScope scope, Guid? flowId, string key, string value, CancellationToken cancellationToken = default)
    {
        var fid = flowId ?? Guid.Empty;
        var existing = await _db.VariableMapEntries
            .FirstOrDefaultAsync(e => e.Scope == (int)scope && e.FlowId == fid && e.Key == key, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.Value = value;
        }
        else
        {
            _db.VariableMapEntries.Add(new VariableMapEntry
            {
                Id = Guid.NewGuid(),
                Scope = (int)scope,
                FlowId = fid,
                Key = key,
                Value = value,
            });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(VariableMapScope scope, Guid? flowId, CancellationToken cancellationToken = default)
    {
        var fid = flowId ?? Guid.Empty;
        return await _db.VariableMapEntries
            .Where(e => e.Scope == (int)scope && e.FlowId == fid)
            .ToDictionaryAsync(e => e.Key, e => e.Value, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RemoveAsync(VariableMapScope scope, Guid? flowId, string key, CancellationToken cancellationToken = default)
    {
        var fid = flowId ?? Guid.Empty;
        await _db.VariableMapEntries
            .Where(e => e.Scope == (int)scope && e.FlowId == fid && e.Key == key)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
