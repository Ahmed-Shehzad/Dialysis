using System.Reflection;
using Dialysis.DomainDrivenDesign.Primitives;
using Shouldly;
using Xunit;

namespace Dialysis.ArchitectureTests;

/// <summary>
/// Per Eric Evans, <em>Domain-Driven Design</em> (2003) pp. 88–94 and the project's DDD-Alignment standard,
/// an aggregate root protects its invariants by exposing behaviour methods rather than open setters. This
/// gate reflects over every concrete <see cref="AggregateRoot{TId}"/> subclass loaded into the test process
/// and asserts that no instance property has a public setter.
/// </summary>
public sealed class AggregateRootEncapsulationTests
{
    [Fact]
    public void Aggregate_roots_must_not_expose_public_setters()
    {
        var aggregateRoots = ModuleAssemblyLoader.LoadAll()
            .SelectMany(SafeGetTypes)
            .Where(t => t is { IsClass: true, IsAbstract: false } && InheritsAggregateRoot(t))
            .ToList();

        aggregateRoots.ShouldNotBeEmpty("Expected at least one AggregateRoot subclass.");

        var offenders = new List<string>();
        foreach (var t in aggregateRoots)
        {
            foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var setter = prop.GetSetMethod(nonPublic: false);
                if (setter is not null && !setter.IsPrivate && !setter.IsFamily && !IsInitOnly(setter))
                {
                    offenders.Add($"{t.FullName}.{prop.Name}");
                }
            }
        }

        offenders.ShouldBeEmpty($"Public setters detected on aggregate-root properties:\n  - {string.Join("\n  - ", offenders)}");
    }

    private static bool InheritsAggregateRoot(Type t)
    {
        var current = t.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static bool IsInitOnly(MethodInfo setter) =>
        setter.ReturnParameter.GetRequiredCustomModifiers()
            .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");

    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try
        { return a.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
    }
}
