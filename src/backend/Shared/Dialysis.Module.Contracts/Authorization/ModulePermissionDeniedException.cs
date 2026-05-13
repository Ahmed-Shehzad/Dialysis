namespace Dialysis.Module.Contracts.Authorization;

/// <summary>Thrown when the current principal lacks a required module permission (maps to HTTP 403).</summary>
public sealed class ModulePermissionDeniedException(string permission, string? userId)
    : Exception($"Missing permission '{permission}' for user '{userId ?? "(anonymous)"}'.")
{
    public string Permission { get; } = permission;

    public string? UserId { get; } = userId;
}
