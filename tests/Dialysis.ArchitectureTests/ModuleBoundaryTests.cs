using System.Reflection;
using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace Dialysis.ArchitectureTests;

/// <summary>
/// Codifies the long-term boundary rule of the modular monolith:
/// a project belonging to module &lt;X&gt; may only reference its own siblings, the shared layers
/// (DomainDrivenDesign, BuildingBlocks, CQRS, Module.Contracts, Module.Hosting),
/// and the <c>&lt;Y&gt;.Contracts</c> assembly of any other module.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly string[] AllowedSharedNamespaceRoots =
    [
        "Dialysis.DomainDrivenDesign",
        "Dialysis.BuildingBlocks",
        "Dialysis.CQRS",
        "Dialysis.Module.Contracts",
        "Dialysis.Module.Hosting",
        "System",
        "Microsoft",
        "Asp.Versioning",
        "OpenTelemetry",
        "Npgsql",
        "Polly",
    ];

    /// <summary>
    /// No project under <c>Dialysis.EHR.*</c> may reference any non-Contracts assembly of <c>Dialysis.PDMS.*</c>.
    /// </summary>
    [Fact]
    public void Ehr_assemblies_must_not_depend_on_PDMS_internals()
    {
        var ehrAssemblies = LoadByPrefix("Dialysis.EHR.");
        foreach (var asm in ehrAssemblies)
        {
            var referenced = asm.GetReferencedAssemblies();
            var violations = referenced
                .Where(r => r.Name is not null
                    && r.Name.StartsWith("Dialysis.PDMS.", StringComparison.Ordinal)
                    && !r.Name.Equals("Dialysis.PDMS.Contracts", StringComparison.Ordinal))
                .Select(r => $"{asm.GetName().Name} -> {r.Name}")
                .ToList();

            violations.ShouldBeEmpty(
                $"EHR assembly {asm.GetName().Name} must not reference non-Contracts PDMS assemblies. Violations: {string.Join(", ", violations)}");
        }
    }

    /// <summary>
    /// No project under <c>Dialysis.PDMS.*</c> may reference any non-Contracts assembly of <c>Dialysis.EHR.*</c>.
    /// </summary>
    [Fact]
    public void Pdms_assemblies_must_not_depend_on_EHR_internals()
    {
        var pdmsAssemblies = LoadByPrefix("Dialysis.PDMS.");
        foreach (var asm in pdmsAssemblies)
        {
            var referenced = asm.GetReferencedAssemblies();
            var violations = referenced
                .Where(r => r.Name is not null
                    && r.Name.StartsWith("Dialysis.EHR.", StringComparison.Ordinal)
                    && !r.Name.Equals("Dialysis.EHR.Contracts", StringComparison.Ordinal))
                .Select(r => $"{asm.GetName().Name} -> {r.Name}")
                .ToList();

            violations.ShouldBeEmpty(
                $"PDMS assembly {asm.GetName().Name} must not reference non-Contracts EHR assemblies. Violations: {string.Join(", ", violations)}");
        }
    }

    /// <summary>
    /// HIS internals must not depend on EHR or PDMS internals (only their Contracts are allowed).
    /// </summary>
    [Fact]
    public void His_assemblies_must_not_depend_on_other_modules_internals()
    {
        var hisAssemblies = LoadByPrefix("Dialysis.HIS.");
        foreach (var asm in hisAssemblies)
        {
            var referenced = asm.GetReferencedAssemblies();
            var violations = referenced
                .Where(r =>
                {
                    if (r.Name is null) return false;
                    if (r.Name.StartsWith("Dialysis.EHR.", StringComparison.Ordinal) && !r.Name.Equals("Dialysis.EHR.Contracts", StringComparison.Ordinal)) return true;
                    if (r.Name.StartsWith("Dialysis.PDMS.", StringComparison.Ordinal) && !r.Name.Equals("Dialysis.PDMS.Contracts", StringComparison.Ordinal)) return true;
                    return false;
                })
                .Select(r => $"{asm.GetName().Name} -> {r.Name}")
                .ToList();

            violations.ShouldBeEmpty(
                $"HIS assembly {asm.GetName().Name} must not reference non-Contracts assemblies of EHR or PDMS. Violations: {string.Join(", ", violations)}");
        }
    }

    /// <summary>
    /// Module Contracts assemblies must be pure: no <c>Microsoft.EntityFrameworkCore</c>,
    /// no <c>Microsoft.AspNetCore</c>, no transport-specific Transponder packages.
    /// Contracts are consumed across module boundaries, so leaking infra dependencies poisons everyone.
    /// </summary>
    [Theory]
    [InlineData("Dialysis.EHR.Contracts")]
    [InlineData("Dialysis.PDMS.Contracts")]
    public void Contracts_assemblies_must_be_infrastructure_free(string contractsAssemblyName)
    {
        var asm = LoadByName(contractsAssemblyName);
        var referenced = asm.GetReferencedAssemblies();

        var disallowedPrefixes = new[]
        {
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "Npgsql",
            "Transponder.Transport",
            "Transponder.Persistence",
        };

        var violations = referenced
            .Where(r => r.Name is not null && disallowedPrefixes.Any(p => r.Name.StartsWith(p, StringComparison.Ordinal)))
            .Select(r => r.Name)
            .ToList();

        violations.ShouldBeEmpty(
            $"{contractsAssemblyName} must remain infrastructure-free. Violations: {string.Join(", ", violations!)}");
    }

    /// <summary>
    /// Domain bounded-context projects (Registration, PatientChart, ..., TreatmentSessions) must not reference EF Core directly.
    /// EF lives in the Persistence layer only — domain depends on ports.
    /// </summary>
    [Fact]
    public void Domain_bounded_contexts_must_not_reference_EntityFrameworkCore()
    {
        var boundedContextAssemblyNames = new[]
        {
            "Dialysis.EHR.Registration",
            "Dialysis.EHR.PatientChart",
            "Dialysis.EHR.Scheduling",
            "Dialysis.EHR.PatientPortal",
            "Dialysis.EHR.ClinicalNotes",
            "Dialysis.EHR.Billing",
            "Dialysis.PDMS.TreatmentSessions",
        };

        foreach (var name in boundedContextAssemblyNames)
        {
            var asm = LoadByName(name);
            var referenced = asm.GetReferencedAssemblies();
            var leak = referenced.FirstOrDefault(r =>
                r.Name is not null && r.Name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
            leak.ShouldBeNull(
                $"Domain bounded context {name} must not reference EntityFrameworkCore directly (leak: {leak?.Name}).");
        }
    }

    private static IReadOnlyList<Assembly> LoadByPrefix(string prefix)
    {
        // Ensure types from these assemblies are eagerly loaded into the test AppDomain.
        // Referencing them in the .csproj is sufficient for the runtime to discover them once we ask for them.
        var baseDir = AppContext.BaseDirectory;
        foreach (var dll in Directory.EnumerateFiles(baseDir, $"{prefix}*.dll"))
        {
            try { Assembly.LoadFrom(dll); } catch { /* best effort */ }
        }
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name is { } n && n.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();
    }

    private static Assembly LoadByName(string assemblyName)
    {
        var match = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.Ordinal));
        if (match is not null) return match;

        var dll = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
        return Assembly.LoadFrom(dll);
    }
}
