using Dialysis.BuildingBlocks.Tefca.Ti.Secrets;
using Dialysis.BuildingBlocks.Tefca.Ti.Smcb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Tefca.Ti;

/// <summary>
/// One-liner composition entry-point for the gematik TI + ePA scaffold. Hosts call this
/// alongside <c>services.AddEuDataProtection("module")</c>. A real SMC-B reader + a real
/// secrets provider must be registered separately by the deployment (the defaults are
/// safe-but-loud stubs).
///
/// Usage:
/// <code>
/// services.AddTelematikInfrastrukturClient(options =>
/// {
///     options.Environment = GematikEnvironment.Test;
/// });
/// // Per-deployment:
/// services.AddSingleton&lt;ISmcBCardReader, PcscSmcBCardReader&gt;();
/// services.AddSingleton&lt;ITiSecretsProvider, AzureKeyVaultTiSecretsProvider&gt;();
/// </code>
/// </summary>
public static class TefcaTiServiceCollectionExtensions
{
    public static IServiceCollection AddTelematikInfrastrukturClient(
        this IServiceCollection services,
        Action<TiOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<TiOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        // Safe defaults — replaced per deployment.
        services.TryAddSingleton<ISmcBCardReader, StubSmcBCardReader>();
        services.TryAddSingleton<ITiSecretsProvider, EnvironmentVariableTiSecretsProvider>();
        services.TryAddSingleton(TimeProvider.System);

        // The TI client always wraps an HttpClient configured by the host's
        // MutualTlsHttpClientFactory. Register the named client so the host can attach the
        // gematik trust-anchor pack + mTLS cert handler.
        services.AddHttpClient<ITelematikInfrastrukturClient, GematikTelematikInfrastrukturClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
