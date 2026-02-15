using Microsoft.Extensions.DependencyInjection;

namespace Verifier;

/// <summary>
/// Extension methods to register Verifier.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Register Verifier validators into the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Callback to configure validator assembly registrations.</param>
    /// <returns>The same service collection instance.</returns>
    public static IServiceCollection AddVerifier(this IServiceCollection services, Action<VerifierBuilder> configure)
    {
        var builder = new VerifierBuilder(services);
        configure(builder);
        builder.Build();
        return services;
    }
}
