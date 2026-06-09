using System.Collections.Concurrent;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Ports;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// In-memory <see cref="ICptFeeScheduleAdminRepository"/> test double for the fee-schedule controller
/// unit tests (the controller's HTTP result behaviour is what's under test, not persistence). Production
/// uses the EF-backed PostgreSQL repository.
/// </summary>
internal sealed class InMemoryCptFeeScheduleAdminRepository : ICptFeeScheduleAdminRepository
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
