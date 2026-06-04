using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Module.Hosting.Authorization;

public sealed class ModuleAuthorizationService : IModuleAuthorizationService
{
    private readonly ICurrentUser _currentUser;
    public ModuleAuthorizationService(ICurrentUser currentUser) => _currentUser = currentUser;
    public Task EnsurePermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        if (_currentUser.Permissions.Contains(permission, StringComparer.Ordinal))
            return Task.CompletedTask;

        throw new ModulePermissionDeniedException(permission, _currentUser.UserId);
    }
}
