using Microsoft.Extensions.DependencyInjection;

namespace Intercessor;

/// <summary>
/// Extension methods to register Intercessor
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Register Intercessor to ServiceCollection
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddIntercessor(this IServiceCollection services, Action<IntercessorBuilder> configure)
    {
        var builder = new IntercessorBuilder(services);
        configure(builder);
        builder.Build();
        return services;
    }
}