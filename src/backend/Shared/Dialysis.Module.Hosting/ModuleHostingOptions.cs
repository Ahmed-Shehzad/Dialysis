using System.Reflection;
using Dialysis.Module.Hosting.Telemetry;

namespace Dialysis.Module.Hosting;

/// <summary>
/// Configures <see cref="ModuleHostingServiceCollectionExtensions.AddModuleHost"/>.
/// Modules pass their slug + permission catalog + handler assemblies; everything else has sensible defaults.
/// </summary>
public sealed class ModuleHostingOptions
{
    /// <summary>
    /// Stable module slug, used for telemetry resource attributes and configuration section keys.
    /// Required.
    /// </summary>
    public required string ModuleSlug { get; init; }

    /// <summary>
    /// Configuration section that binds <see cref="Authorization.ModuleAuthenticationOptions"/>.
    /// Default: <c>"{ModuleSlug}:Authentication"</c>.
    /// </summary>
    public string? AuthenticationConfigurationSection { get; set; }

    /// <summary>Configures telemetry (OTel) for this module.</summary>
    public Action<ModuleTelemetryOptions>? ConfigureTelemetry { get; set; }

    /// <summary>
    /// Assemblies scanned for CQRS handlers, validators, and pipeline behaviors.
    /// Defaults to the calling assembly when none are supplied.
    /// </summary>
    public IReadOnlyCollection<Assembly>? HandlerAssemblies { get; set; }
}
