using Dialysis.SharedKernel.Abstractions;

namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// No-op token provider when SMART OAuth2 is not configured.
/// </summary>
public sealed class NoOpSmartFhirTokenProvider : ISmartFhirTokenProvider
{
    public bool IsConfigured => false;
    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
}
