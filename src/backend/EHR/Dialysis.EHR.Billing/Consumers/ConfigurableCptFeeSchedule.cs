using System.Collections.Concurrent;
using Dialysis.EHR.Billing.Domain;
using Microsoft.Extensions.Configuration;

namespace Dialysis.EHR.Billing.Consumers;

/// <summary>
/// Configuration-driven <see cref="ICptFeeSchedule"/>. Production deployments use an
/// EF-backed schedule that captures per-payer + effective-date rates; the configuration-
/// backed implementation covers dev hosts and any deployment that hasn't yet wired the
/// real fee table. Reads the rate map from the
/// <c>EHR:Billing:CptFeeSchedule:{cptCode}</c> configuration keys; falls back to a
/// dev-default of $250 USD per service-line per CPT when no key is present so the
/// charge consumer never blocks waiting on a fee schedule.
/// </summary>
public sealed class ConfigurableCptFeeSchedule(IConfiguration configuration) : ICptFeeSchedule
{
    private readonly ConcurrentDictionary<string, Money> _cache = new(StringComparer.OrdinalIgnoreCase);

    public Task<Money> LookupAsync(string cptCode, CancellationToken cancellationToken)
    {
        var money = _cache.GetOrAdd(cptCode, code =>
        {
            var section = configuration.GetSection($"EHR:Billing:CptFeeSchedule:{code}");
            var amount = section.GetValue<decimal?>("Amount") ?? 250m;
            var currency = section.GetValue<string>("Currency") ?? "USD";
            return new Money(amount, currency);
        });
        return Task.FromResult(money);
    }
}
