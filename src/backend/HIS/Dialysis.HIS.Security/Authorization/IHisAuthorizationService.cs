namespace Dialysis.HIS.Security.Authorization;

public interface IHisAuthorizationService
{
    Task EnsurePermissionAsync(string permission, CancellationToken cancellationToken = default);
}
