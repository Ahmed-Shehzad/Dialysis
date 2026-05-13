namespace Dialysis.Identity.Provisioning.Domain;

/// <summary>
/// A named bundle of permission strings. Each module owns its own permission catalog; a role
/// defined here can grant any combination of those permission codes across modules.
/// </summary>
public sealed class RoleDefinition
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public List<string> Permissions { get; set; } = [];
}
