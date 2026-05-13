namespace Dialysis.Module.Contracts.Authorization;

/// <summary>
/// Declares the closed set of permission strings a module recognizes.
/// Modules supply a static permission class (e.g. <c>HisPermissions</c>) and a thin
/// <see cref="IModulePermissionCatalog"/> wrapper around it so the hosting layer can
/// validate incoming claims without taking a hard reference to the module.
/// </summary>
public interface IModulePermissionCatalog
{
    /// <summary>Stable module slug used as a prefix in permission strings (e.g. <c>"his"</c>, <c>"ehr"</c>).</summary>
    string ModuleSlug { get; }

    /// <summary>All permissions this module recognizes (must include every value emitted by handlers).</summary>
    IReadOnlyCollection<string> All { get; }
}
