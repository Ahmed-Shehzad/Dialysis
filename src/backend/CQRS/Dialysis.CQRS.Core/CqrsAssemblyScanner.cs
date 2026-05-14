using System.Reflection;
using Dialysis.BuildingBlocks.Intercessor;
using Dialysis.BuildingBlocks.Verifier;
using Dialysis.CQRS.Commands;
using Dialysis.CQRS.Queries;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace Dialysis.CQRS;

internal static class CqrsAssemblyScanner
{
    private static readonly MethodInfo _addHandlerMethod =
        typeof(IntercessorBuilder).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(static m =>
                m.Name == nameof(IntercessorBuilder.AddHandler) &&
                m is { IsGenericMethodDefinition: true } &&
                m.GetGenericArguments() is [{ Name: "TRequest" }, { Name: "TResponse" }, { Name: "THandler" }]);

    internal static void Register(IntercessorBuilder inner, IServiceCollection services, Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(services);

        var distinct = assemblies.Where(a => a is not null).Distinct().ToArray();
        if (distinct.Length == 0)
            return;

        RegisterHandlers(inner, distinct);
        RegisterValidatorsScrutor(services, distinct);
    }

    private static void RegisterHandlers(IntercessorBuilder inner, Assembly[] assemblies)
    {
        var registered = new HashSet<(Type Request, Type Response, Type Handler)>();

        foreach (var assembly in assemblies)
        {
            foreach (var impl in GetConcreteTypes(assembly))
                RegisterHandlerImplementations(inner, impl, registered);
        }
    }

    private static void RegisterHandlerImplementations(
        IntercessorBuilder inner,
        Type impl,
        HashSet<(Type Request, Type Response, Type Handler)> registered)
    {
        foreach (var iface in impl.GetInterfaces())
        {
            if (!TryGetCqrsHandlerContract(iface, out var request, out var response))
                continue;

            if (!registered.Add((request, response, impl)))
                continue;

            _addHandlerMethod
                .MakeGenericMethod(request, response, impl)
                .Invoke(inner, null);
        }
    }

    private static bool TryGetCqrsHandlerContract(Type iface, out Type request, out Type response)
    {
        if (!iface.IsGenericType)
        {
            request = null!;
            response = null!;
            return false;
        }

        var gd = iface.GetGenericTypeDefinition();
        if (gd != typeof(IQueryHandler<,>) && gd != typeof(ICommandHandler<,>))
        {
            request = null!;
            response = null!;
            return false;
        }

        request = iface.GenericTypeArguments[0];
        response = iface.GenericTypeArguments[1];
        return true;
    }

    private static void RegisterValidatorsScrutor(IServiceCollection services, Assembly[] assemblies)
    {
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.Where(IsConcreteCqrsValidator))
            .UsingRegistrationStrategy(RegistrationStrategy.Append)
            .AsImplementedInterfaces(IsCqrsValidatorServiceContract)
            .WithScopedLifetime());
    }

    private static bool IsCqrsValidatorServiceContract(Type serviceType) =>
        serviceType.IsGenericType &&
        serviceType.GetGenericTypeDefinition() == typeof(IValidator<>) &&
        IsCqrsMessageType(serviceType.GenericTypeArguments[0]);

    private static bool IsConcreteCqrsValidator(Type type)
    {
        if (!type.IsClass || type.IsAbstract)
            return false;

        return type.GetInterfaces().Any(static iface =>
            iface.IsGenericType &&
            iface.GetGenericTypeDefinition() == typeof(IValidator<>) &&
            IsCqrsMessageType(iface.GenericTypeArguments[0]));
    }

    private static bool IsCqrsMessageType(Type messageType)
    {
        if (typeof(ICommand).IsAssignableFrom(messageType))
            return true;

        foreach (var iface in messageType.GetInterfaces())
        {
            if (!iface.IsGenericType)
                continue;

            var gd = iface.GetGenericTypeDefinition();
            if (gd == typeof(IQuery<>))
                return true;
            if (gd == typeof(ICommand<>))
                return true;
        }

        return false;
    }

    private static IEnumerable<Type> GetConcreteTypes(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
        }

        foreach (var type in types)
        {
            if (type.IsClass && !type.IsAbstract)
                yield return type;
        }
    }
}
