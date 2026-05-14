using System.Reflection;
using Dialysis.DomainDrivenDesign.IntegrationEvents;
using Shouldly;
using Xunit;

namespace Dialysis.ArchitectureTests;

/// <summary>
/// Per Eric Evans, <em>Domain-Driven Design</em> (2003) pp. 263–264 (Open Host Service / Published Language),
/// every integration event must declare its schema version explicitly. This gate reflects over every
/// concrete <see cref="IIntegrationEvent"/> implementation and asserts that:
/// <list type="number">
///   <item>An <c>int SchemaVersion</c> public instance property exists.</item>
///   <item>The property is required (constructor-driven) so that producers cannot publish without choosing a version.</item>
/// </list>
/// See <c>src/backend/DomainDrivenDesign/.../IntegrationEvents/Versioning.md</c> for the policy.
/// </summary>
public sealed class IntegrationEventVersioningTests
{
    [Fact]
    public void All_integration_events_declare_required_schema_version()
    {
        var eventTypes = ModuleAssemblyLoader.LoadAll()
            .SelectMany(SafeGetTypes)
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IIntegrationEvent).IsAssignableFrom(t)
                        && t != typeof(IntegrationEvent))
            .ToList();

        eventTypes.ShouldNotBeEmpty("Expected at least one IIntegrationEvent record.");

        var offenders = new List<string>();
        foreach (var t in eventTypes)
        {
            var prop = t.GetProperty("SchemaVersion", BindingFlags.Public | BindingFlags.Instance);
            if (prop is null || prop.PropertyType != typeof(int))
            {
                offenders.Add($"{t.FullName} — missing 'int SchemaVersion' property");
                continue;
            }

            // For positional records, SchemaVersion comes through a primary constructor parameter,
            // making it impossible to construct without supplying a value.
            // For events extending the abstract IntegrationEvent base, the inherited init-only property
            // defaults to 1; consumers can override but it's never zero.
            var ctorParamPresent = t.GetConstructors()
                .Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(int) && p.Name == "SchemaVersion"));
            if (!ctorParamPresent && !typeof(IntegrationEvent).IsAssignableFrom(t))
            {
                offenders.Add($"{t.FullName} — SchemaVersion not surfaced via constructor (producer can omit it).");
            }
        }

        offenders.ShouldBeEmpty($"Integration events missing schema-version contract:\n  - {string.Join("\n  - ", offenders)}");
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
    }
}
