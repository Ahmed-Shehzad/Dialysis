using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Module.Hosting.Authorization;

public sealed class ModuleAuthorizationService(ICurrentUser currentUser) : IModuleAuthorizationService
{
    public Task EnsurePermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        if (currentUser.Permissions.Contains(permission, StringComparer.Ordinal))
            return Task.CompletedTask;

        throw new ModulePermissionDeniedException(permission, currentUser.UserId);
    }
}
