namespace Dialysis.BuildingBlocks.Tefca.Ti.Secrets;

/// <summary>
/// Resolves runtime secrets the TI client needs that don't live on the SMC-B (e.g. the
/// gematik IDP client_secret for service-account flows, the OAuth refresh-token wrapper).
/// Production deployments wire a vault-backed provider (Azure Key Vault, AWS Secrets
/// Manager, HashiCorp Vault). Dev / CI uses <see cref="EnvironmentVariableTiSecretsProvider"/>.
///
/// Never log a secret's value. Implementations should not cache for longer than the secret's
/// natural refresh window (rotation policy lives at the vault).
/// </summary>
public interface ITiSecretsProvider
{
    /// <summary>Resolve a named secret. Returns null when not configured.</summary>
    Task<string?> GetAsync(string secretKey, CancellationToken cancellationToken);
}

/// <summary>Dev / CI provider: reads from <c>SMARTCONNECT_TI_*</c> environment variables.</summary>
public sealed class EnvironmentVariableTiSecretsProvider : ITiSecretsProvider
{
    public Task<string?> GetAsync(string secretKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);
        var envVar = "SMARTCONNECT_TI_" + secretKey.ToUpperInvariant().Replace('.', '_').Replace('-', '_');
        var value = Environment.GetEnvironmentVariable(envVar);
        return Task.FromResult(string.IsNullOrEmpty(value) ? null : value);
    }
}
