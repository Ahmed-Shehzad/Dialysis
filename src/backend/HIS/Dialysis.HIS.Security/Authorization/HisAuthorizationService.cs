using Dialysis.HIS.Security;

namespace Dialysis.HIS.Security.Authorization;

public sealed class HisAuthorizationService(ICurrentUser currentUser) : IHisAuthorizationService
{
    public Task EnsurePermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        if (currentUser.Permissions.Contains(permission, StringComparer.Ordinal))
            return Task.CompletedTask;

        throw new HisPermissionDeniedException(permission, currentUser.UserId);
    }
}
