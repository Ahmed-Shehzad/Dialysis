using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Dialysis.Module.Hosting.Resilience;

public static class ResilientHttpClientExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds a named <see cref="HttpClient"/> with a Polly retry policy (3 attempts, exponential backoff)
        /// for cross-module HTTP / gRPC fallbacks. Use sparingly — domain events should travel over Transponder.
        /// </summary>
        public IHttpClientBuilder AddResilientModuleHttpClient(
            string name,
            Action<HttpClient>? configureClient = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var builder = services.AddHttpClient(name);
            if (configureClient is not null)
                builder.ConfigureHttpClient(configureClient);

            builder.AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: static attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))));

            return builder;
        }
    }
}
