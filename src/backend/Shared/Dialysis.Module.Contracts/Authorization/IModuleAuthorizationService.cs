namespace Dialysis.Module.Contracts.Authorization;

/// <summary>
/// Domain-level authorization check used by pipeline behaviors and ad-hoc handler code.
/// Implementations throw <see cref="ModulePermissionDeniedException"/> when the permission is missing.
/// </summary>
public interface IModuleAuthorizationService
{
    Task EnsurePermissionAsync(string permission, CancellationToken cancellationToken = default);
}
