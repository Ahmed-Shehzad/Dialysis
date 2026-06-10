using System.Reflection;
using Dialysis.Module.Contracts.Authorization;
using Shouldly;
using Xunit;

namespace Dialysis.ArchitectureTests;

/// <summary>
/// Authorization in the modular monolith is centralized in
/// <c>Dialysis.Module.Hosting.Pipeline.AuthorizationPipelineBehavior</c>, which only runs when a
/// dispatched request implements <see cref="IPermissionedCommand"/>. A command or query that omits
/// the marker therefore silently bypasses every permission check — exactly the defect that let the
/// HIS billing-export queries leak payer data to any authenticated caller.
///
/// This gate reflects over every concrete <c>ICommand&lt;&gt;</c> / <c>IQuery&lt;&gt;</c> contract in the
/// module assemblies and asserts each one carries the permission marker, so the gap cannot recur.
/// </summary>
public sealed class PermissionedRequestTests
{
    // Module assemblies only — building-block/CQRS infrastructure (gateways, the interface
    // definitions themselves, durable-bus envelopes) are out of scope for permission gating.
    private static readonly string[] _moduleSegments =
    [
        ".HIS.", ".EHR.", ".PDMS.", ".HIE.", ".Identity.", ".SmartConnect.", ".Lab.",
    ];

    private const string CommandInterface = "Dialysis.CQRS.Commands.ICommand`1";
    private const string QueryInterface = "Dialysis.CQRS.Queries.IQuery`1";

    [Fact]
    public void All_module_commands_and_queries_declare_a_required_permission()
    {
        var requestTypes = ModuleAssemblyLoader.LoadAll()
            .Where(a => a.GetName().Name is { } name
                        && _moduleSegments.Any(seg => name.Contains(seg, StringComparison.Ordinal))
                        && !name.EndsWith(".Tests", StringComparison.Ordinal))
            .SelectMany(SafeGetTypes)
            .Where(t => t is { IsClass: true, IsAbstract: false } && ImplementsCommandOrQuery(t))
            .ToList();

        requestTypes.ShouldNotBeEmpty("Expected at least one ICommand<>/IQuery<> contract in the module assemblies.");

        var offenders = requestTypes
            .Where(t => !typeof(IPermissionedCommand).IsAssignableFrom(t))
            .Select(t => t.FullName!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        offenders.ShouldBeEmpty(
            "Commands/queries that bypass AuthorizationPipelineBehavior (add IPermissionedCommand + RequiredPermission):\n  - "
            + string.Join("\n  - ", offenders));
    }

    private static bool ImplementsCommandOrQuery(Type t) =>
        t.GetInterfaces().Any(i => i.IsGenericType
            && i.GetGenericTypeDefinition().FullName is CommandInterface or QueryInterface);

    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try
        { return a.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
    }
}
