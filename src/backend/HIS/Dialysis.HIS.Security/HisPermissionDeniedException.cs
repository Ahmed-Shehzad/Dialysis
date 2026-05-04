namespace Dialysis.HIS.Security;

/// <summary>Thrown when the current principal lacks a required HIS permission (maps to HTTP 403).</summary>
public sealed class HisPermissionDeniedException(string permission, string? userId)
    : Exception($"Missing permission '{permission}' for user '{userId ?? "(anonymous)"}'.")
{
    public string Permission { get; } = permission;

    public string? UserId { get; } = userId;
}
