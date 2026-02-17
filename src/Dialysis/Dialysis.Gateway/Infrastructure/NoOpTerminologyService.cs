using Dialysis.SharedKernel.Abstractions;

namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// No-op terminology service. Returns null for all lookups. Phase 4.3.2.
/// Replace with Refit-based implementation when external terminology service is available.
/// </summary>
public sealed class NoOpTerminologyService : ITerminologyService
{
    public Task<string?> LookupDisplayAsync(string system, string code, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
