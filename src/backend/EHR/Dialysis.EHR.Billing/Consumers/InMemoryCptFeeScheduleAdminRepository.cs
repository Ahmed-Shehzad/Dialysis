using System.Collections.Concurrent;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;

namespace Dialysis.EHR.Billing.Consumers;

/// <summary>
/// Process-scoped in-memory <see cref="ICptFeeScheduleAdminRepository"/> for dev hosts running
/// the billing slice with <c>EHR:Billing:Persistence:Provider=InMemory</c>. Production registers
/// the EF-backed variant so edits survive restarts and are shared across replicas. Add/Remove
/// mutate the backing store immediately, so the caller's <c>SaveChangesAsync</c> is a harmless
/// no-op for this implementation.
/// </summary>
public sealed class InMemoryCptFeeScheduleAdminRepository : ICptFeeScheduleAdminRepository
{
    private readonly ConcurrentDictionary<Guid, CptFeeScheduleEntry> _entries = new();

    public Task<IReadOnlyList<CptFeeScheduleEntry>> ListAsync(
        string? cptCode, string? payerCode, CancellationToken cancellationToken = default)
    {
        IEnumerable<CptFeeScheduleEntry> rows = _entries.Values;
        if (!string.IsNullOrWhiteSpace(cptCode))
        {
            var code = cptCode.Trim().ToUpperInvariant();
            rows = rows.Where(e => e.CptCode == code);
        }
        if (!string.IsNullOrWhiteSpace(payerCode))
        {
            var payer = payerCode.Trim().ToUpperInvariant();
            rows = rows.Where(e => e.PayerCode == payer);
        }
        rows = rows
            .OrderBy(e => e.CptCode, StringComparer.Ordinal)
            .ThenBy(e => e.PayerCode, StringComparer.Ordinal)
            .ThenByDescending(e => e.EffectiveFromUtc);
        return Task.FromResult<IReadOnlyList<CptFeeScheduleEntry>>([.. rows]);
    }

    public Task<CptFeeScheduleEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_entries.TryGetValue(id, out var entry) ? entry : null);

    public void Add(CptFeeScheduleEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entries[entry.Id] = entry;
    }

    public void Remove(CptFeeScheduleEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entries.TryRemove(entry.Id, out _);
    }
}
