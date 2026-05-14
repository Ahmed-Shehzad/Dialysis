using Hl7.Fhir.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Fhir.Terminology;

/// <summary>
/// Decorates an <see cref="ITerminologyService"/> with an in-process TTL cache. Concept lookups and
/// validate-code calls dominate hot-path mapper traffic; caching them halts the upstream round-trip
/// for repeated reads of stable codes (LOINC / SNOMED / RxNorm concept properties do not change
/// within a release).
///
/// Cache entries are sized at <c>1</c> each so <see cref="FhirTerminologyOptions.CacheSizeLimit"/>
/// caps total resident entries (not bytes).
/// </summary>
public sealed class MemoryCacheTerminologyDecorator : ITerminologyService
{
    private readonly ITerminologyService _inner;
    private readonly IMemoryCache _cache;
    private readonly FhirTerminologyOptions _options;

    public MemoryCacheTerminologyDecorator(
        ITerminologyService inner,
        IMemoryCache cache,
        IOptions<FhirTerminologyOptions> options)
    {
        _inner = inner;
        _cache = cache;
        _options = options.Value;
    }

    public ValueTask<Parameters> LookupAsync(string system, string code, CancellationToken cancellationToken) =>
        GetOrAddAsync(
            key: $"lookup|{system}|{code}",
            ttl: _options.LookupCacheTtl,
            factory: ct => _inner.LookupAsync(system, code, ct),
            cancellationToken);

    public ValueTask<Parameters> ValidateCodeAsync(string valueSetUrl, string code, string? system, CancellationToken cancellationToken) =>
        GetOrAddAsync(
            key: $"validate|{valueSetUrl}|{system ?? "*"}|{code}",
            ttl: _options.ValidateCodeCacheTtl,
            factory: ct => _inner.ValidateCodeAsync(valueSetUrl, code, system, ct),
            cancellationToken);

    public ValueTask<Parameters> TranslateAsync(string conceptMapUrl, string sourceSystem, string sourceCode, CancellationToken cancellationToken) =>
        GetOrAddAsync(
            key: $"translate|{conceptMapUrl}|{sourceSystem}|{sourceCode}",
            ttl: _options.TranslateCacheTtl,
            factory: ct => _inner.TranslateAsync(conceptMapUrl, sourceSystem, sourceCode, ct),
            cancellationToken);

    public ValueTask<ValueSet> ExpandAsync(string valueSetUrl, IReadOnlyDictionary<string, string> filters, CancellationToken cancellationToken)
    {
        var filterFragment = filters.Count == 0
            ? string.Empty
            : "|" + string.Join('&', filters.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key}={kv.Value}"));
        return GetOrAddAsync(
            key: $"expand|{valueSetUrl}{filterFragment}",
            ttl: _options.ExpandCacheTtl,
            factory: ct => _inner.ExpandAsync(valueSetUrl, filters, ct),
            cancellationToken);
    }

    private async ValueTask<TValue> GetOrAddAsync<TValue>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, ValueTask<TValue>> factory,
        CancellationToken cancellationToken)
        where TValue : class
    {
        if (_options.CacheSizeLimit <= 0)
            return await factory(cancellationToken).ConfigureAwait(false);

        if (_cache.TryGetValue<TValue>(key, out var cached) && cached is not null)
            return cached;

        var value = await factory(cancellationToken).ConfigureAwait(false);
        using var entry = _cache.CreateEntry(key);
        entry.Value = value;
        entry.AbsoluteExpirationRelativeToNow = ttl;
        entry.Size = 1;
        return value;
    }
}
