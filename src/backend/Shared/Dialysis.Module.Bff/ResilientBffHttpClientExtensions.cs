using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Dialysis.Module.Bff;

/// <summary>
/// Resilient named-<see cref="HttpClient"/> registration for BFF hosts. Mirrors
/// <c>Dialysis.Module.Hosting</c>'s <c>AddResilientModuleHttpClient</c> without dragging the full
/// module-hosting stack into the BFF tier.
/// </summary>
public static class ResilientBffHttpClientExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds a named <see cref="HttpClient"/> with a Polly transient-error retry policy
        /// (3 attempts, exponential backoff). Used for the Keycloak token/userinfo client —
        /// every session refresh in the system rides on it, so a broker blip must not bounce
        /// a clinician back through login when a retry would have succeeded.
        /// </summary>
        public IHttpClientBuilder AddResilientBffHttpClient(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            return services.AddHttpClient(name)
                .AddPolicyHandler(HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: static attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))));
        }
    }
}
