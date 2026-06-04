namespace Dialysis.Module.Contracts.Authorization;

/// <summary>Thrown when the current principal lacks a required module permission (maps to HTTP 403).</summary>
public sealed class ModulePermissionDeniedException : Exception
{
    /// <summary>Thrown when the current principal lacks a required module permission (maps to HTTP 403).</summary>
    public ModulePermissionDeniedException(string permission, string? userId) : base($"Missing permission '{permission}' for user '{userId ?? "(anonymous)"}'.")
    {
        Permission = permission;
        UserId = userId;
    }
    public string Permission { get; }

    public string? UserId { get; }
}
