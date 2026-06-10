using System.Reflection;
using System.Text.Json;
using Dialysis.Module.Contracts.Authorization;
using Shouldly;
using Xunit;

namespace Dialysis.ArchitectureTests;

/// <summary>
/// The Keycloak realm (<c>src/backend/Identity/keycloak/dialysis-realm.json</c>) hands every BFF a
/// hardcoded <c>dialysis_permission</c> claim through the <c>dev-dialysis-permissions</c> protocol
/// mapper. That list is hand-maintained and copied across one mapper per BFF client, so it silently
/// drifts behind the code every time a module adds a permission constant — the symptom is a runtime
/// <c>403 Forbidden — Missing permission '…'</c> for a permission the API enforces but no token carries.
///
/// This gate makes the drift a build failure instead: it unions every <see cref="IModulePermissionCatalog"/>
/// the modules expose and asserts each mapper's claim is a superset. Adding a permission constant without
/// updating the realm now fails here, naming exactly which permissions are missing from which mapper.
/// </summary>
public sealed class KeycloakRealmPermissionTests
{
    private const string MapperName = "dev-dialysis-permissions";
    private const string ClaimName = "dialysis_permission";

    [Fact]
    public void Every_realm_permission_mapper_covers_all_module_catalog_permissions()
    {
        var catalogPermissions = DiscoverCatalogPermissions();
        catalogPermissions.ShouldNotBeEmpty(
            "Expected at least one IModulePermissionCatalog in the module assemblies — did the catalog assemblies fail to load?");

        var mappers = LoadRealmPermissionMappers();
        mappers.ShouldNotBeEmpty(
            $"Expected at least one '{MapperName}' mapper emitting the '{ClaimName}' claim in {RealmFilePath()}.");

        var offenders = mappers
            .Select(m => (m.Client, Missing: catalogPermissions.Except(m.Permissions).OrderBy(p => p, StringComparer.Ordinal).ToList()))
            .Where(x => x.Missing.Count > 0)
            .Select(x => $"{x.Client}: {string.Join(", ", x.Missing)}")
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        offenders.ShouldBeEmpty(
            "Keycloak realm claim drift — these permissions exist in module catalogs but are missing from the "
            + $"'{MapperName}' mapper's '{ClaimName}' claim. Add them to every mapper in "
            + "src/backend/Identity/keycloak/dialysis-realm.json:\n  - "
            + string.Join("\n  - ", offenders));
    }

    private static SortedSet<string> DiscoverCatalogPermissions()
    {
        var permissions = new SortedSet<string>(StringComparer.Ordinal);

        var catalogTypes = ModuleAssemblyLoader.LoadAll()
            .SelectMany(SafeGetTypes)
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IModulePermissionCatalog).IsAssignableFrom(t)
                        && t.GetConstructor(Type.EmptyTypes) is not null);

        foreach (var type in catalogTypes)
        {
            var catalog = (IModulePermissionCatalog)Activator.CreateInstance(type)!;
            foreach (var permission in catalog.All)
                permissions.Add(permission);
        }

        return permissions;
    }

    private static IReadOnlyList<(string Client, HashSet<string> Permissions)> LoadRealmPermissionMappers()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(RealmFilePath()));
        var result = new List<(string, HashSet<string>)>();

        if (!doc.RootElement.TryGetProperty("clients", out var clients))
            return result;

        foreach (var client in clients.EnumerateArray())
        {
            var clientId = client.TryGetProperty("clientId", out var id) ? id.GetString() ?? "?" : "?";
            if (!client.TryGetProperty("protocolMappers", out var mappers))
                continue;

            foreach (var mapper in mappers.EnumerateArray())
            {
                if (!mapper.TryGetProperty("name", out var name) || name.GetString() != MapperName)
                    continue;
                if (!mapper.TryGetProperty("config", out var config))
                    continue;
                if (!config.TryGetProperty("claim.name", out var claim) || claim.GetString() != ClaimName)
                    continue;
                if (!config.TryGetProperty("claim.value", out var value) || value.GetString() is not { } raw)
                    continue;

                // claim.value is a JSON array serialized into a string.
                var permissions = JsonSerializer.Deserialize<string[]>(raw) ?? [];
                result.Add((clientId, [.. permissions]));
            }
        }

        return result;
    }

    private static string RealmFilePath()
    {
        const string relative = "src/backend/Identity/keycloak/dialysis-realm.json";
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException($"Could not locate '{relative}' walking up from {AppContext.BaseDirectory}.");
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try
        { return a.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
    }
}
