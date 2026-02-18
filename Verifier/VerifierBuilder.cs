using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Verifier.Abstractions;

namespace Verifier;

/// <summary>
/// Provides a fluent builder for configuring Verifier validator registrations.
/// </summary>
public sealed class VerifierBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<Assembly> _assemblies = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="VerifierBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public VerifierBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Adds an assembly for validator registrations.
    /// </summary>
    /// <param name="assembly">The assembly to register.</param>
    public void RegisterFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _assemblies.Add(assembly);
    }

    internal void Build()
    {
        if (_assemblies.Count == 0) return;

        var distinctAssemblies = _assemblies.Distinct().ToList();

        _ = _services.Scan(scan => scan
            .FromAssemblies(distinctAssemblies)
            .AddClasses(classes => classes.AssignableTo(typeof(IValidator<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime()
        );
    }
}
