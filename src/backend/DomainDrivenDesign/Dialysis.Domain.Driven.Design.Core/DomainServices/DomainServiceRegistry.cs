using System.Reflection;
using Dialysis.DomainDrivenDesign.DomainServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.DomainDrivenDesign.DomainServices;

/// <summary>
/// Scans an assembly for concrete <see cref="IDomainService"/> implementations and registers each
/// as scoped against itself and every domain-service interface it implements.
/// </summary>
public static class DomainServiceRegistry
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var implementations = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(t => typeof(IDomainService).IsAssignableFrom(t));

        foreach (var implementation in implementations)
        {
            services.TryAddScoped(implementation);
            foreach (var contract in implementation.GetInterfaces()
                         .Where(i => typeof(IDomainService).IsAssignableFrom(i) && i != typeof(IDomainService)))
            {
                services.TryAddEnumerable(ServiceDescriptor.Scoped(contract, implementation));
            }
        }

        return services;
    }
}
