using System.Reflection;

namespace Dialysis.ArchitectureTests;

/// <summary>
/// Loads every <c>Dialysis.*.dll</c> assembly found alongside the test assembly so reflection-based
/// architecture gates can see all module types regardless of which entry-point types are touched first.
/// </summary>
internal static class ModuleAssemblyLoader
{
    private static IReadOnlyList<Assembly>? _cached;

    public static IReadOnlyList<Assembly> LoadAll()
    {
        if (_cached is not null)
            return _cached;

        var here = Path.GetDirectoryName(typeof(ModuleAssemblyLoader).Assembly.Location)!;
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name is { } n && n.StartsWith("Dialysis.", StringComparison.Ordinal))
            .ToDictionary(a => a.GetName().Name!, a => a);

        foreach (var dll in Directory.EnumerateFiles(here, "Dialysis.*.dll"))
        {
            var name = Path.GetFileNameWithoutExtension(dll);
            if (loaded.ContainsKey(name))
                continue;
            try
            {
                var asm = Assembly.LoadFrom(dll);
                loaded[asm.GetName().Name!] = asm;
            }
            catch
            {
                // Skip assemblies that fail to load; the gates surface that as a missing type.
            }
        }

        _cached = [.. loaded.Values];
        return _cached;
    }
}
