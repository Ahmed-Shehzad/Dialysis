using Dialysis.SharedKernel.Abstractions;

namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// No-op de-identification. Returns input unchanged. Phase 2.2.3.
/// Replace with external service (e.g. ARX, Privacy Analytics) when needed.
/// </summary>
public sealed class NoOpDeidentificationService : IDeidentificationService
{
    public Task<string?> DeidentifyAsync(string fhirBundleJson, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(fhirBundleJson);
}
