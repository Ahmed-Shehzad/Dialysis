using System.Globalization;
using System.Reflection;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;

namespace Dialysis.HIS.Api.OpenApi;

/// <summary>
/// Registers one OpenAPI document per ApiExplorer group name (same format string as <c>AddApiExplorer</c> <c>GroupNameFormat</c>).
/// Group names are derived from <see cref="ApiVersionAttribute"/> on MVC controllers so new versions pick up documents automatically.
/// </summary>
public static class HisOpenApiDocuments
{
    /// <summary>Must match <c>AddApiExplorer(o =&gt; o.GroupNameFormat = ...)</c>.</summary>
    public const string ExplorerGroupNameFormat = "'v'VVV";

    public static IServiceCollection AddHisVersionedOpenApi(this IServiceCollection services)
    {
        foreach (var documentName in DiscoverExplorerGroupNames())
        {
            var name = documentName;
            services.AddOpenApi(name, options =>
            {
                options.ShouldInclude = description =>
                    string.IsNullOrEmpty(description.GroupName) ||
                    string.Equals(description.GroupName, name, StringComparison.Ordinal);
            });
        }

        return services;
    }

    private static IReadOnlyList<string> DiscoverExplorerGroupNames()
    {
        var versions = new HashSet<ApiVersion>();
        foreach (var type in typeof(HisOpenApiDocuments).Assembly.GetTypes())
        {
            if (!IsConcreteMvcController(type))
                continue;

            CollectApiVersions(type, versions);
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                CollectApiVersions(method, versions);
        }

        if (versions.Count == 0)
            return [new ApiVersion(1, 0).ToString(ExplorerGroupNameFormat, CultureInfo.InvariantCulture)];

        return versions
            .Select(v => v.ToString(ExplorerGroupNameFormat, CultureInfo.InvariantCulture))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void CollectApiVersions(MemberInfo member, HashSet<ApiVersion> versions)
    {
        foreach (var a in member.GetCustomAttributes<ApiVersionAttribute>(inherit: true))
        {
            foreach (var v in a.Versions)
                versions.Add(v);
        }
    }

    private static bool IsConcreteMvcController(Type type) =>
        type is { IsAbstract: false, IsClass: true } && typeof(ControllerBase).IsAssignableFrom(type);
}
