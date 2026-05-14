using System.Reflection;
using Dialysis.DomainDrivenDesign.DomainEvents;
using Dialysis.DomainDrivenDesign.DomainServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.DomainDrivenDesign.CompositionRoot;

/// <summary>
/// Registers DDD defaults (dispatcher, save-changes interceptor, domain-service scan)
/// for a module's composition root.
/// </summary>
public static class DomainDrivenDesignServiceCollectionExtensions
{
    /// <summary>
    /// Registers the in-memory <see cref="IDomainEventDispatcher"/> and the
    /// <see cref="DomainEventSaveChangesInterceptor"/>. The interceptor must additionally be
    /// added to the module's <c>DbContextOptions</c> in <c>OnConfiguring</c> / <c>AddDbContext</c>.
    /// Handlers run in a fresh DI scope (post-commit) so they can use their own DbContext.
    /// </summary>
    public static IServiceCollection AddDomainEventDispatch(this IServiceCollection services)
    {
        services.TryAddScoped<IDomainEventDispatcher, InMemoryDomainEventDispatcher>();
        services.TryAddScoped<DomainEventSaveChangesInterceptor>();
        return services;
    }

    /// <summary>
    /// Convenience wrapper: registers domain-event dispatch and scans the given assemblies for
    /// <see cref="DomainEvents.IDomainEventHandler{TEvent}"/> implementations and
    /// <see cref="IDomainService"/> implementations.
    /// </summary>
    public static IServiceCollection AddDomainDrivenDesignCore(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddDomainEventDispatch();

        foreach (var assembly in assemblies)
        {
            services.AddDomainServices(assembly);
            RegisterDomainEventHandlers(services, assembly);
        }

        return services;
    }

    private static void RegisterDomainEventHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerInterface = typeof(IDomainEventHandler<>);

        var implementations = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

        foreach (var implementation in implementations)
        {
            var closedHandlerInterfaces = implementation.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface);

            foreach (var contract in closedHandlerInterfaces)
            {
                services.TryAddEnumerable(ServiceDescriptor.Scoped(contract, implementation));
            }
        }
    }
}
